using EducationalPlataform.Data;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using EducationalPlataform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Claims;

namespace EducationalPlataform.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CertificatesController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;
        private readonly CertificateEligibilityService _eligibility;

        public CertificatesController(EducationalPlataformContext context, CertificateEligibilityService eligibility)
        {
            _context = context;
            _eligibility = eligibility;
        }

        [HttpGet("mine")]
        public async Task<IActionResult> Mine()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var enrollments = await _context.CourseEnrollments
                .AsNoTracking()
                .Include(e => e.Course)
                .Where(e => e.UserId == userId)
                .ToListAsync();

            var issued = await _context.Certificates
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var list = new List<object>();
            foreach (var enrollment in enrollments)
            {
                var eval = await _eligibility.EvaluateAsync(userId, enrollment.CourseId);
                var cert = issued.FirstOrDefault(c => c.CourseId == enrollment.CourseId);
                list.Add(new
                {
                    enrollment.CourseId,
                    CourseTitle = enrollment.Course?.Title,
                    ProgressPercentage = enrollment.ProgressPercentage,
                    eval.Eligible,
                    eval.Reason,
                    eval.ExamAverage,
                    Issued = cert != null,
                    Code = cert?.Code,
                    IssuedAt = cert?.IssuedAt
                });
            }

            return Ok(list);
        }

        [HttpGet]
        [Authorize(Roles = "Coordinator,Teacher")]
        public async Task<IActionResult> StaffList()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var query = _context.Certificates
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.Course)
                .AsQueryable();

            var role = User.FindFirstValue(ClaimTypes.Role);
            if (string.Equals(role, nameof(UserProfile.Teacher), StringComparison.OrdinalIgnoreCase))
                query = query.Where(c => c.Course.TeacherId == userId);

            var items = await query
                .OrderByDescending(c => c.IssuedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Code,
                    c.IssuedAt,
                    c.ExamAverage,
                    Student = c.User.UserName,
                    CourseTitle = c.Course.Title,
                    c.CourseId
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("issue/{courseId:int}")]
        public async Task<IActionResult> Issue(int courseId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var eval = await _eligibility.EvaluateAsync(userId, courseId);
            if (!eval.Eligible)
                return BadRequest(new { message = eval.Reason });

            var existing = await _context.Certificates
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CourseId == courseId);

            if (existing != null)
                return Ok(new { existing.Code, existing.IssuedAt, alreadyIssued = true });

            var certificate = new Certificate
            {
                UserId = userId,
                CourseId = courseId,
                Code = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant(),
                IssuedAt = DateTime.Now,
                ExamAverage = eval.ExamAverage
            };
            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();
            return Ok(new { certificate.Code, certificate.IssuedAt, alreadyIssued = false });
        }

        [HttpGet("{code}/pdf")]
        public async Task<IActionResult> DownloadPdf(string code)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var certificate = await _context.Certificates
                .Include(c => c.User)
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.Code == code);

            if (certificate == null)
                return NotFound();

            var role = User.FindFirstValue(ClaimTypes.Role);
            var isStaff = string.Equals(role, nameof(UserProfile.Coordinator), StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, nameof(UserProfile.Teacher), StringComparison.OrdinalIgnoreCase);

            if (!isStaff && certificate.UserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden);

            if (string.Equals(role, nameof(UserProfile.Teacher), StringComparison.OrdinalIgnoreCase)
                && certificate.Course.TeacherId != userId)
                return StatusCode(StatusCodes.Status403Forbidden);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.Content().Column(col =>
                    {
                        col.Item().AlignCenter().Text("Certificado de Conclusão").FontSize(28).Bold();
                        col.Item().PaddingTop(20).AlignCenter().Text("Certificamos que").FontSize(14);
                        col.Item().PaddingTop(8).AlignCenter().Text(certificate.User.UserName ?? "Aluno").FontSize(22).Bold();
                        col.Item().PaddingTop(12).AlignCenter().Text($"concluiu o curso {certificate.Course.Title}").FontSize(16);
                        col.Item().PaddingTop(8).AlignCenter().Text($"com média {certificate.ExamAverage:0.#}% nas provas.").FontSize(14);
                        col.Item().PaddingTop(24).AlignCenter().Text($"Código: {certificate.Code}  •  Emitido em {certificate.IssuedAt:dd/MM/yyyy}");
                    });
                });
            });

            var pdf = document.GeneratePdf();
            return File(pdf, "application/pdf", $"certificado-{certificate.Code}.pdf");
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out userId);
        }
    }
}
