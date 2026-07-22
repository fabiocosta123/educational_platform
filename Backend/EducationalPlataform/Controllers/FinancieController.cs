using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EducationalPlataform.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FinancieController : Controller
    {
        private readonly EducationalPlataformContext _context; 

        public FinancieController(EducationalPlataformContext context)
        {
            _context = context;
        }

        //gera cobrança pix
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
            await _context.SaveChangesAsync();

            return Ok(response);
        }

        //Confirma pagamento PIX (via webhook do PSP)
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
            await _context.SaveChangesAsync();

            return Ok("Pagamento confirmado e curso liberado.");
        }
    }
}
