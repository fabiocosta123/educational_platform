using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Controllers
{

    
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


        // lista todos os pagamentos
        [Authorize(Roles = "Coordenador")]
        [HttpGet("pix")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments(
            int? userId = null,
            int? courseId = null,
            string status = null)
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

            var payments = await query.ToListAsync();

            return Ok(payments);
        }


        // historico financeiro
        [Authorize(Roles = "Coordenador")]
        [HttpGet("pix/history")]
        public async Task<ActionResult<object>> GetFinancialHistory()
        {
            var totalRecebido = await _context.Payments
            .Where(p => p.Status == "Pago")
            .SumAsync(p => p.Amount);

            var totalPendente = await _context.Payments
                .Where(p => p.Status == "Pendente")
                .SumAsync(p => p.Amount);

            var totalPagamentos = await _context.Payments.CountAsync();
            var pagos = await _context.Payments.Where(p => p.Status == "Pago").CountAsync();
            var pendentes = totalPagamentos - pagos;

            var inadimplencia = totalPagamentos > 0
                ? (decimal)pendentes / totalPagamentos * 100
                : 0;

            var resumo = new
            {
                TotalRecebido = totalRecebido,
                TotalPendente = totalPendente,
                TotalPagamentos = totalPagamentos,
                Pagos = pagos,
                Pendentes = pendentes,
                TaxaInadimplencia = inadimplencia
            };

            return Ok(resumo);
        }

        // auditoria de pagamento
        [Authorize(Roles = "Coordenador")]
        [HttpGet("pix/audit/{paymentId}")]
        public async Task<ActionResult<IEnumerable<PaymentAudit>>> GetPaymentAudit(int paymentId)
        {
            var audits = await _context.PaymentAudits
                .Where(a => a.PaymentId == paymentId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            return Ok(audits);
        }


        //gera cobrança pix
        [Authorize(Roles = "Coordenador")]
        [HttpPost("pix")]
        public async Task<ActionResult<PixPaymentDto.PixPaymentResponseDto>> GeneratePix([FromBody] PixPaymentDto.PixPaymentRequestDto dto)
        {
            // chamar api do psp (mercado pago, pag seguro etc)
            var response = new PixPaymentDto.PixPaymentResponseDto
            {
                QrCodeBase64 = "mocked_qrcode_base64",
                CopiaCola = "000000000002023123433314br.gov.bsb.pix...",
                Status = "Pendente"
            };

            var payment = new Payment
            {
                UserId = dto.UserId,
                CourseId = dto.CourseId,
                Amount = dto.Amount,
                Status = "Pendente"
            };

            _context.Payments.Add(payment);
            await RegisterAudit(payment.Id, "Criado", $"Cobrança PIX gerada para User {dto.UserId}, Curso {dto.CourseId}, Valor {dto.Amount}");

            await _context.SaveChangesAsync();

            return Ok(response);
        }

        //Confirma pagamento PIX (via webhook do PSP)
        [Authorize(Roles = "Coordenador")]
        [HttpPost("pix/confirm/{paymentId}")]
        public async Task<IActionResult> ConfirmPixPayment(int paymentId)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment == null) return NotFound();

            payment.Status = "Pago";
            payment.PaidAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Libera curso para aluno
            var enrollment = new CourseEnrollment
            {
                UserId = payment.UserId,
                CourseId = payment.CourseId,
                Status = "Ativo",
                ProgressPercentage = 0
            };
            _context.CourseEnrollments.Add(enrollment);
            await RegisterAudit(payment.Id, "Confirmado", $"Pagamento confirmado manualmente para Payment {payment.Id}");

            await _context.SaveChangesAsync();

            return Ok("Pagamento confirmado e curso liberado.");
        }

        // webhook psp (publico)
        [AllowAnonymous]
        [HttpPost("pix/webhook")]
        public async Task<IActionResult> PixWebhook([FromBody] PixWebhookDto dto)
        {
            // localiza pagamento
            var payment = await _context.Payments.FindAsync(dto.PaymentId);

            if (payment == null) return NotFound();

            // Atualiza status
            payment.Status = "Pago";
            payment.PaidAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Libera curso para aluno
            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.UserId == payment.UserId && e.CourseId == payment.CourseId);

            if (enrollment == null)
            {
                enrollment = new CourseEnrollment
                {
                    UserId = payment.UserId,
                    CourseId = payment.CourseId,
                    Status = "Ativo",
                    ProgressPercentage = 0
                };
                _context.CourseEnrollments.Add(enrollment);
            }
            else
            {
                enrollment.Status = "Ativo";
            }

            await RegisterAudit(payment.Id, "WebhookRecebido", $"Pagamento confirmado pelo PSP. TransactionId: {dto.TransactionId}");

            await _context.SaveChangesAsync();

            return Ok(new { message = "Pagamento confirmado via webhook e curso liberado." });

        }
    }
}
