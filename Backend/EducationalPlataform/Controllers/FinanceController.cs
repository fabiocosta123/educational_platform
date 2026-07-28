using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
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


        #region Metodos Privados
        private void RegisterAudit(int paymentId, string action, string details)
        {
            var audit = new PaymentAudit
            {
                PaymentId = paymentId,
                Action = action,
                Details = details
            };

            _context.PaymentAudits.Add(audit);            
        }



        private async Task<FinanceSummaryDto> BuildFinanceSummary(IQueryable<Payment> query, int activeStudents)
        {

            var now = DateTime.Now;

            var totalPayments = await query.CountAsync();

            var totalReceived = await query
                .Where(p => p.Status == PaymentStatus.Paid)
                .SumAsync(p => p.Amount);

            var totalPending = await query
                .Where(p => p.Status == PaymentStatus.Pending)
                .SumAsync(p => p.Amount);

            var monthlyRevenue = await query
                .Where(p =>
                    p.Status == PaymentStatus.Paid &&
                    p.PaidAt.HasValue &&
                    p.PaidAt.Value.Month == now.Month &&
                    p.PaidAt.Value.Year == now.Year)
                .SumAsync(p => p.Amount);

            var paid = await query.CountAsync(p => p.Status == PaymentStatus.Paid);

            var pending = totalPayments - paid;

            return new FinanceSummaryDto
            {
                TotalReceived = totalReceived,
                TotalPending = totalPending,
                TotalPayments = totalPayments,
                Paid = paid,
                Pending = pending,
                DefaultRate = totalPayments == 0
                    ? 0
                    : (decimal)pending / totalPayments * 100,
                ActiveStudents = activeStudents,
                MonthlyRevenue = monthlyRevenue,
                Message = totalPayments == 0
                    ? "Nenhum dado financeiro encontrado."
                    : null
            };
        }


        private IQueryable<dynamic> BuildPaymentHistory(IQueryable<Payment> query)
        {
            return query
                .Include(p => p.User)
                .Include(p => p.Course)
                .ThenInclude(c => c.Teacher)
                .OrderBy(p => p.DueDate)
                .Select(p => new
                {
                    p.Id,
                    p.Amount,
                    p.Status,
                    p.DueDate,
                    p.PaidAt,

                    Course = new
                    {
                        Title = p.Course.Title,
                        Teacher = p.Course.Teacher != null
                            ? p.Course.Teacher.UserName
                            : null
                    },

                    Student = new
                    {
                        UserName = p.User.UserName
                    }
                });
        }

        #endregion



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

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<PaymentStatus>(status, true, out var paymentStatus))
            {
                query = query.Where(p => p.Status == paymentStatus);
            }

            if (!string.IsNullOrEmpty(userName))
                query = query.Where(p => p.User.UserName.Contains(userName));

            return Ok(await query.ToListAsync());
        }

        // Resumo financeiro geral
        #region Consultas

        [HttpGet("pix/history")]
        public async Task<IActionResult> GetFinancialHistory()
        {
            var activeStudents = await _context.CourseEnrollments
                .Where(e => e.Status == "Active")
                .Select(e => e.UserId)
                .Distinct()
                .CountAsync();

            var query = _context.Payments.AsQueryable();

            var summary = await BuildFinanceSummary(query, activeStudents);

            var payments = await BuildPaymentHistory(query).ToListAsync();

            return Ok(new
            {
                summary,
                payments
            });
        }


        [HttpGet("pix/student")]
        public async Task<IActionResult> GetStudentFinancialHistory([FromQuery] string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return BadRequest("userName is required");

            var query = _context.Payments
                .Where(p => p.User.UserName.Contains(userName));

            if (!await query.AnyAsync())
                return NotFound($"No payments found for student {userName}");

            var summary = await BuildFinanceSummary(query, 1);

            var payments = await BuildPaymentHistory(query).ToListAsync();

            return Ok(new
            {
                summary,
                payments
            });
        }
        #endregion




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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == dto.UserName);
            if (user == null)
                return NotFound(new { message = $"O aluno '{dto.UserName}' não foi encontrado." });

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Title == dto.CourseTitle);
            if (course == null)
                return NotFound(new { message = $"O curso '{dto.CourseTitle}' não existe." });

            var payment = new Payment
            {
                UserId = user.Id,
                CourseId = course.Id,
                Amount = dto.Amount,
                Status = PaymentStatus.Pending,
                DueDate = dto.DueDate
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(); // salva primeiro para gerar o Id

            RegisterAudit(payment.Id, "Created", $"PIX charge generated for {dto.UserName}, Curso {dto.CourseTitle}, Valor {dto.Amount}");
            await _context.SaveChangesAsync(); // salva auditoria

            var baseUrl = $"{Request.Scheme}://{Request.Host}/api/finance";
            var response = new PixPaymentDto.PixPaymentResponseDto
            {
                QrCodeBase64 = null,
                CopiaCola = $"00020126580014BR.GOV.BCB.PIX0136{payment.Id}520400005303986540{payment.Amount}5802BR5925{user.UserName}",
                Status = payment.Status.ToString(),
                DownloadUrl = $"{baseUrl}/pix/download/{payment.Id}",
                DownloadPdfUrl = $"{baseUrl}/pix/download/pdf/{payment.Id}"
            };

            return Ok(response);
        }





        // Confirma pagamento PIX
        [HttpPost("pix/confirm/{paymentId}")]
        public async Task<IActionResult> ConfirmPixPayment(int paymentId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var payment = await _context.Payments.FindAsync(paymentId);
                if (payment == null) return NotFound();

                if (payment.Status == PaymentStatus.Paid)
                    return BadRequest("Pagamento já confirmado.");

                // Atualiza status e data
                payment.Status = PaymentStatus.Paid;
                payment.PaidAt = DateTime.Now;

                // Cria matrícula
                var enrollment = new CourseEnrollment
                {
                    UserId = payment.UserId,
                    CourseId = payment.CourseId,
                    Status = "Active",
                    ProgressPercentage = 0
                };
                _context.CourseEnrollments.Add(enrollment);

                // Registra auditoria
                RegisterAudit(payment.Id, "Confirmed", $"Payment {payment.Id} confirmed manually");

                // Salva tudo de uma vez
                await _context.SaveChangesAsync();

                // Confirma transação
                await transaction.CommitAsync();

                return Ok("Payment confirmed and course unlocked.");
            }
            catch (Exception ex)
            {
                // Reverte caso algo dê errado
                await transaction.RollbackAsync();
                return StatusCode(500, $"Erro ao confirmar pagamento: {ex.Message}");
            }
        }



        // Webhook PSP
        [AllowAnonymous]
        [HttpPost("pix/webhook")]
        public async Task<IActionResult> PixWebhook([FromBody] PixWebhookDto dto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var payment = await _context.Payments.FindAsync(dto.PaymentId);
                if (payment == null) return NotFound();

                // Atualiza status e data
                payment.Status = PaymentStatus.Paid;
                payment.PaidAt = DateTime.Now;

                // Verifica matrícula
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

                // Registra auditoria
                RegisterAudit(payment.Id, "WebhookReceived", $"Payment confirmed by PSP. TransactionId: {dto.TransactionId}");

                // Salva tudo de uma vez
                await _context.SaveChangesAsync();

                // Confirma transação
                await transaction.CommitAsync();

                return Ok(new { message = "Payment confirmed via webhook and course unlocked." });
            }
            catch (Exception ex)
            {
                // Reverte tudo se der erro
                await transaction.RollbackAsync();
                return StatusCode(500, $"Erro ao processar webhook: {ex.Message}");
            }
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
