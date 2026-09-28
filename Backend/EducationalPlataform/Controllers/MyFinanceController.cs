using EducationalPlataform.Data;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using EducationalPlataform.Services;
using EducationalPlataform.Validation;
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
        private readonly MyCreditClient _myCredit;
        private readonly PaymentSettlementService _settlement;

        public MyFinanceController(
            EducationalPlataformContext context,
            MyCreditClient myCredit,
            PaymentSettlementService settlement)
        {
            _context = context;
            _myCredit = myCredit;
            _settlement = settlement;
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
        public async Task<IActionResult> GeneratePix(int paymentId, CancellationToken cancellationToken)
        {
            var payment = await LoadOwnPaymentAsync(paymentId);
            if (payment == null)
                return NotFound(new { message = "Cobrança não encontrada." });

            if (payment.Status == PaymentStatus.Paid)
                return BadRequest(new { message = "Esta mensalidade já está paga." });

            if (payment.Status == PaymentStatus.Cancelled)
                return BadRequest(new { message = "Esta cobrança foi cancelada." });

            if (!_myCredit.IsConfigured)
            {
                return StatusCode(503, new
                {
                    message = "MyCredit ainda não está configurada neste ambiente. Peça ao administrador para definir MyCredit__Cnpj e MyCredit__ResellerToken."
                });
            }

            var document = CpfValidator.DigitsOnly(payment.User.CPF);
            if (document.Length is not 11 and not 14)
            {
                return BadRequest(new { message = "O aluno precisa ter CPF válido no cadastro para emitir o PIX." });
            }

            var stillValid = !string.IsNullOrWhiteSpace(payment.PixCopyPaste)
                && !string.IsNullOrWhiteSpace(payment.PixInvoiceId)
                && payment.PixExpiresAt is DateTime expires
                && expires > DateTime.UtcNow.AddMinutes(2);

            if (!stillValid)
            {
                var invoiceId = Guid.NewGuid().ToString();
                var charge = await _myCredit.CreatePixChargeAsync(
                    invoiceId,
                    payment.Amount,
                    payment.User.UserName ?? "Aluno",
                    document,
                    cancellationToken);

                payment.PixInvoiceId = invoiceId;
                payment.PixTransactionId = charge.TransactionId;
                payment.PixCopyPaste = charge.CopyPaste;
                payment.PixExpiresAt = charge.ExpiresAt;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return Ok(PixPayload(payment, "Pague com o QR ou o copia e cola. Depois clique em Já paguei."));
        }

        [HttpPost("{paymentId:int}/pix/status")]
        public async Task<IActionResult> ConfirmPixStatus(int paymentId, CancellationToken cancellationToken)
        {
            var payment = await LoadOwnPaymentAsync(paymentId);
            if (payment == null)
                return NotFound(new { message = "Cobrança não encontrada." });

            if (payment.Status == PaymentStatus.Paid)
            {
                return Ok(new { paid = true, message = "Pagamento já confirmado." });
            }

            if (string.IsNullOrWhiteSpace(payment.PixInvoiceId) || !_myCredit.IsConfigured)
            {
                return Ok(new { paid = false, message = "Ainda não há cobrança MyCredit para consultar." });
            }

            var paid = await _myCredit.IsPaidAsync(payment.PixInvoiceId, cancellationToken);
            if (!paid)
            {
                return Ok(new { paid = false, message = "Pagamento ainda não identificado. Tente de novo em alguns segundos." });
            }

            await _settlement.ApplyPaidAndUnlockAsync(
                payment,
                DateTime.Now,
                "MyCreditPaid",
                $"PIX confirmado na MyCredit. Fatura {payment.PixInvoiceId}.");
            await _context.SaveChangesAsync(cancellationToken);

            return Ok(new { paid = true, message = "Pagamento confirmado. O curso foi liberado." });
        }

        private async Task<Payment?> LoadOwnPaymentAsync(int paymentId)
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var userId))
                return null;

            return await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId);
        }

        private static object PixPayload(Payment payment, string message) => new
        {
            paymentId = payment.Id,
            amount = payment.Amount,
            courseTitle = payment.Course.Title,
            qrCodeBase64 = PixQrHelper.ToBase64Png(payment.PixCopyPaste!),
            copiaCola = payment.PixCopyPaste,
            provider = "mycredit",
            expiresAt = payment.PixExpiresAt,
            message
        };
    }
}
