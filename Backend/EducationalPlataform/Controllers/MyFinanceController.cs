using EducationalPlataform.Data;
using EducationalPlataform.Models.Enums;
using EducationalPlataform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EducationalPlataform.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/me/finance")]
    public class MyFinanceController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;

        public MyFinanceController(EducationalPlataformContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMine()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
                return Unauthorized();

            var today = DateTime.Today;
            var payments = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Course)
                .Where(p => p.UserId == userId)
                .OrderBy(p => p.DueDate)
                .Select(p => new
                {
                    p.Id,
                    p.Amount,
                    Status = p.Status.ToString(),
                    p.DueDate,
                    p.PaidAt,
                    p.SettledAt,
                    CourseTitle = p.Course.Title,
                    Bucket = p.Status == PaymentStatus.Paid
                        ? "paid"
                        : p.Status == PaymentStatus.Cancelled
                            ? "cancelled"
                            : p.DueDate.HasValue && p.DueDate.Value.Date < today
                                ? "overdue"
                                : p.DueDate.HasValue && p.DueDate.Value.Date >= today
                                    ? "upcoming"
                                    : "open"
                })
                .ToListAsync();

            return Ok(new
            {
                overdue = payments.Where(p => p.Bucket == "overdue").ToList(),
                open = payments.Where(p => p.Bucket == "open").ToList(),
                upcoming = payments.Where(p => p.Bucket == "upcoming").ToList(),
                paid = payments.Where(p => p.Bucket == "paid").ToList(),
                cancelled = payments.Where(p => p.Bucket == "cancelled").ToList()
            });
        }

        [HttpPost("{paymentId:int}/pix")]
        public async Task<IActionResult> GeneratePix(int paymentId)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
                return Unauthorized();

            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);

            if (payment == null)
                return NotFound(new { message = "Cobrança não encontrada." });

            if (payment.Status == PaymentStatus.Paid)
                return BadRequest(new { message = "Esta mensalidade já está paga." });

            if (payment.Status == PaymentStatus.Cancelled)
                return BadRequest(new { message = "Esta cobrança foi cancelada." });

            var pixCode = PixQrHelper.BuildCopyPaste(payment.Id, payment.Amount, payment.User.UserName);
            return Ok(new
            {
                paymentId = payment.Id,
                amount = payment.Amount,
                courseTitle = payment.Course.Title,
                qrCodeBase64 = PixQrHelper.ToBase64Png(pixCode),
                copiaCola = pixCode,
                provider = "placeholder",
                message = "PIX provisório da plataforma. A baixa automática entra quando a MyCredit confirmar o pagamento no webhook."
            });
        }
    }
}
