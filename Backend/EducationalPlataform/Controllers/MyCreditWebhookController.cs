using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using EducationalPlataform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/mycredit")]
public class MyCreditWebhookController : ControllerBase
{
    private readonly EducationalPlataformContext _context;
    private readonly PaymentSettlementService _settlement;

    public MyCreditWebhookController(
        EducationalPlataformContext context,
        PaymentSettlementService settlement)
    {
        _context = context;
        _settlement = settlement;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Receive([FromBody] MyCreditWebhookDto dto)
    {
        var invoiceId = dto.Dados?.Pagamento?.IdFaturaPag;
        if (string.IsNullOrWhiteSpace(invoiceId))
            return Ok(new { received = true, ignored = "idFaturaPag ausente" });

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.PixInvoiceId == invoiceId);

        if (payment == null)
            return Ok(new { received = true, ignored = "cobrança não encontrada" });

        var tipo = dto.Tipo ?? Request.Headers["X-MyCredit-Evento"].ToString();

        if (string.Equals(tipo, "pix.pago", StringComparison.OrdinalIgnoreCase))
        {
            if (payment.Status != PaymentStatus.Paid)
            {
                await _settlement.ApplyPaidAndUnlockAsync(
                    payment,
                    DateTime.Now,
                    "MyCreditWebhookPaid",
                    $"Webhook {dto.Id}. txid {dto.Dados?.Pagamento?.TxId}.");
                await _context.SaveChangesAsync();
            }

            return Ok(new { received = true });
        }

        if (string.Equals(tipo, "pix.estornado", StringComparison.OrdinalIgnoreCase))
        {
            if (payment.Status == PaymentStatus.Cancelled)
                return Ok(new { received = true, ignored = "já estornado" });

            payment.Status = PaymentStatus.Cancelled;
            _context.PaymentAudits.Add(new PaymentAudit
            {
                PaymentId = payment.Id,
                Action = "MyCreditRefunded",
                Details = $"Webhook {dto.Id}. Estorno MyCredit."
            });
            await _context.SaveChangesAsync();
            return Ok(new { received = true });
        }

        return Ok(new { received = true, ignored = tipo });
    }
}
