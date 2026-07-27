using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace EducationalPlataform.Controllers
{
    [Authorize(Roles = "Coordinator")]
    [ApiController]
    [Route("api/[controller]")]
    public class FinanceController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;

        public FinanceController(EducationalPlataformContext context)
        {
            _context = context;
        }

        private async Task RegisterAudit(int paymentId, string action, string details)
        {
            var audit = new PaymentAudit
            {
                PaymentId = paymentId,
                Action = action,
                Details = details
            };

            _context.PaymentAudits.Add(audit);
            await _context.SaveChangesAsync();
        }

        // Lista todos os pagamentos
        [HttpGet("pix")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments(
            int? userId = null,
            int? courseId = null,
            string status = null,
            string userName = null)
        {
            var query = _context.Payments
                .Include(p => p.User)
                .Include(p => p.Course)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(p => p.UserId == userId.Value);

            if (courseId.HasValue)
                query = query.Where(p => p.CourseId == courseId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            if (!string.IsNullOrEmpty(userName))
                query = query.Where(p => p.User.UserName.Contains(userName));

            return Ok(await query.ToListAsync());
        }

        // Resumo financeiro geral
        [HttpGet("pix/history")]
        public async Task<ActionResult<FinanceSummaryDto>> GetFinancialHistory()
        {
            var activeStudents = await _context.CourseEnrollments
                .Where(e => e.Status == "Active")
                .Select(e => e.UserId)
                .Distinct()
                .CountAsync();

            var totalPayments = await _context.Payments.CountAsync();

            if (totalPayments == 0)
            {
                return Ok(new FinanceSummaryDto
                {
                    TotalReceived = 0,
                    TotalPending = 0,
                    TotalPayments = 0,
                    Paid = 0,
                    Pending = 0,
                    DefaultRate = 0,
                    ActiveStudents = activeStudents,
                    MonthlyRevenue = 0,
                    Message = "Nenhum dado financeiro encontrado."
                });
            }

            var totalReceived = await _context.Payments
                .Where(p => p.Status == "Paid")
                .SumAsync(p => p.Amount);

            var totalPending = await _context.Payments
                .Where(p => p.Status == "Pending")
                .SumAsync(p => p.Amount);

            var monthlyRevenue = await _context.Payments
                .Where(p => p.Status == "Paid" && p.PaidAt.HasValue && p.PaidAt.Value.Month == DateTime.Now.Month)
                .SumAsync(p => p.Amount);

            var paid = await _context.Payments.Where(p => p.Status == "Paid").CountAsync();
            var pending = totalPayments - paid;
            var defaultRate = totalPayments > 0 ? (decimal)pending / totalPayments * 100 : 0;

            return Ok(new FinanceSummaryDto
            {
                TotalReceived = totalReceived,
                TotalPending = totalPending,
                TotalPayments = totalPayments,
                Paid = paid,
                Pending = pending,
                DefaultRate = defaultRate,
                ActiveStudents = activeStudents,
                MonthlyRevenue = monthlyRevenue
            });
        }

        // Resumo financeiro individual
        [HttpGet("pix/student")]
        public async Task<ActionResult<FinanceSummaryDto>> GetStudentFinancialHistory([FromQuery] string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return BadRequest("userName is required");

            var query = _context.Payments
                .Include(p => p.User)
                .Where(p => p.User.UserName.Contains(userName));

            if (!await query.AnyAsync())
                return NotFound($"No payments found for student {userName}");

            var totalReceived = await query.Where(p => p.Status == "Paid").SumAsync(p => p.Amount);
            var totalPending = await query.Where(p => p.Status == "Pending").SumAsync(p => p.Amount);
            var totalPayments = await query.CountAsync();
            var paid = await query.Where(p => p.Status == "Paid").CountAsync();
            var pending = totalPayments - paid;
            var defaultRate = totalPayments > 0 ? (decimal)pending / totalPayments * 100 : 0;

            var monthlyRevenue = await query
                .Where(p => p.Status == "Paid" && p.PaidAt.HasValue && p.PaidAt.Value.Month == DateTime.Now.Month)
                .SumAsync(p => p.Amount);

            return Ok(new FinanceSummaryDto
            {
                TotalReceived = totalReceived,
                TotalPending = totalPending,
                TotalPayments = totalPayments,
                Paid = paid,
                Pending = pending,
                DefaultRate = defaultRate,
                ActiveStudents = 1,
                MonthlyRevenue = monthlyRevenue
            });
        }

        // Auditoria de pagamento
        [HttpGet("pix/audit/{paymentId}")]
        public async Task<ActionResult<IEnumerable<PaymentAudit>>> GetPaymentAudit(int paymentId)
        {
            var audits = await _context.PaymentAudits
                .Where(a => a.PaymentId == paymentId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            return Ok(audits);
        }

        // Gera cobrança PIX
        [HttpPost("pix")]
        public async Task<ActionResult<PixPaymentDto.PixPaymentResponseDto>> GeneratePix([FromBody] PixPaymentDto.PixPaymentRequestDto dto)
        {
            // Buscar usuário pelo nome
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == dto.UserName);
            if (user == null)
                return NotFound(new { message = $"O aluno '{dto.UserName}' não foi encontrado. Verifique o nome informado." });

            // Buscar curso pelo título
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Title == dto.CourseTitle);
            if (course == null)
                return NotFound(new { message = $"O curso '{dto.CourseTitle}' não existe. Confira o título informado." });

            if (!decimal.TryParse(dto.Amount.ToString(), NumberStyles.Any, new CultureInfo("pt-BR"), out var amount))
                return BadRequest(new { message = "Valor inválido. Use o formato 99,99 ou 99.99." });

            var payment = new Payment
            {
                UserId = user.Id,
                CourseId = course.Id,
                Amount = amount,
                Status = "Pending",
                DueDate = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            await RegisterAudit(payment.Id, "Created", $"PIX charge generated for {dto.UserName}, Curso {dto.CourseTitle}, Valor {dto.Amount}");

            var baseUrl = $"{Request.Scheme}://{Request.Host}/api/finance";
            var response = new PixPaymentDto.PixPaymentResponseDto
            {
                QrCodeBase64 = null, 
                CopiaCola = $"00020126580014BR.GOV.BCB.PIX0136{payment.Id}520400005303986540{payment.Amount}5802BR5925{user.UserName}",
                Status = payment.Status,
                DownloadUrl = $"{baseUrl}/pix/download/{payment.Id}",
                DownloadPdfUrl = $"{baseUrl}/pix/download/pdf/{payment.Id}"

            };

            


            return Ok(response);
        }



        // Confirma pagamento PIX
        [HttpPost("pix/confirm/{paymentId}")]
        public async Task<IActionResult> ConfirmPixPayment(int paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) return NotFound();

            payment.Status = "Paid";
            payment.PaidAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var enrollment = new CourseEnrollment
            {
                UserId = payment.UserId,
                CourseId = payment.CourseId,
                Status = "Active",
                ProgressPercentage = 0
            };
            _context.CourseEnrollments.Add(enrollment);

            await RegisterAudit(payment.Id, "Confirmed", $"Payment {payment.Id} confirmed manually");
            await _context.SaveChangesAsync();

            return Ok("Payment confirmed and course unlocked.");
        }

        // Webhook PSP
        [AllowAnonymous]
        [HttpPost("pix/webhook")]
        public async Task<IActionResult> PixWebhook([FromBody] PixWebhookDto dto)
        {
            var payment = await _context.Payments.FindAsync(dto.PaymentId);
            if (payment == null) return NotFound();

            payment.Status = "Paid";
            payment.PaidAt = DateTime.Now;
            await _context.SaveChangesAsync();

            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.UserId == payment.UserId && e.CourseId == payment.CourseId);

            if (enrollment == null)
            {
                enrollment = new CourseEnrollment
                {
                    UserId = payment.UserId,
                    CourseId = payment.CourseId,
                    Status = "Active",
                    ProgressPercentage = 0
                };
                _context.CourseEnrollments.Add(enrollment);
            }
            else
            {
                enrollment.Status = "Active";
            }

            await RegisterAudit(payment.Id, "WebhookReceived", $"Payment confirmed by PSP. TransactionId: {dto.TransactionId}");
            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment confirmed via webhook and course unlocked." });
        }

        // Download QR Code em PNG
        [HttpGet("pix/download/{paymentId}")]
        public async Task<IActionResult> DownloadPix(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                return NotFound("Pagamento não encontrado.");

            // campo CopiaCola ou montar a string PIX
            var pixCode = $"00020126580014BR.GOV.BCB.PIX0136{payment.Id}520400005303986540{payment.Amount}5802BR5925{payment.User.UserName}";

            // Gerar QR Code com QRCoder
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(pixCode, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrCodeData);
            using var qrBitmap = qrCode.GetGraphic(20);

            using var stream = new MemoryStream();
            qrBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            var qrBytes = stream.ToArray();

            return File(qrBytes, "image/png", $"pix_payment_{payment.Id}.png");
        }

        // Download QR Code em PDF
        [HttpGet("pix/download/pdf/{paymentId}")]
        public async Task<IActionResult> DownloadPixPdf(int paymentId)
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                return NotFound("Pagamento não encontrado.");

            // Gera QR Code em bytes
            var pixCode = $"00020126580014BR.GOV.BCB.PIX0136{payment.Id}520400005303986540{payment.Amount}5802BR5925{payment.User.UserName}";
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(pixCode, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrCodeData);
            using var qrBitmap = qrCode.GetGraphic(20);
            using var ms = new MemoryStream();
            qrBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var qrBytes = ms.ToArray();

            // Monta PDF com QuestPDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.Header().Text("Cobrança PIX").FontSize(20).Bold();
                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Aluno: {payment.User.UserName}");
                        col.Item().Text($"Curso: {payment.Course?.Title}");
                        col.Item().Text($"Valor: R$ {payment.Amount}");
                        col.Item().Text($"Vencimento: {payment.DueDate?.ToString("dd/MM/yyyy") ?? "Não definido"}");
                        col.Item().Text($"Status: {payment.Status}");

                        col.Item().Image(qrBytes);
                    });
                });
            });

            var pdfBytes = document.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"pix_payment_{payment.Id}.pdf");
        }


        [HttpGet("debug-claims")]
        public IActionResult DebugClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(claims);
        }

    }
}
