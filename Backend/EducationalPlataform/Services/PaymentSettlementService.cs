using EducationalPlataform.Data;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Services;

public sealed class PaymentSettlementService
{
    private readonly EducationalPlataformContext _context;
    private readonly LatePaymentPolicyService _latePolicy;

    public PaymentSettlementService(EducationalPlataformContext context, LatePaymentPolicyService latePolicy)
    {
        _context = context;
        _latePolicy = latePolicy;
    }

    public async Task ApplyPaidAndUnlockAsync(
        Payment payment,
        DateTime paidAt,
        string auditAction,
        string auditDetails)
    {
        var chargedLateFee = !payment.LateFeeApplied;
        _latePolicy.EnsureLateFee(payment, paidAt);
        if (chargedLateFee && payment.LateFeeApplied)
        {
            _context.PaymentAudits.Add(new PaymentAudit
            {
                PaymentId = payment.Id,
                Action = "LateFeeApplied",
                Details = $"Multa de atraso de {LatePaymentPolicyService.LateFeeAmount:0.00} aplicada uma vez."
            });
        }

        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = paidAt;
        payment.SettledAt = DateTime.Now;

        var enrollment = await _context.CourseEnrollments
            .FirstOrDefaultAsync(e =>
                e.UserId == payment.UserId &&
                e.CourseId == payment.CourseId);

        var remainingOverdue = await _context.Payments
            .Where(p =>
                p.UserId == payment.UserId &&
                p.CourseId == payment.CourseId &&
                p.Id != payment.Id &&
                p.Status == PaymentStatus.Pending)
            .ToListAsync();
        var stillBlocked = remainingOverdue.Any(p =>
            LatePaymentPolicyService.IsBlockingOverdue(p, DateTime.Today));
        var nextStatus = stillBlocked
            ? LatePaymentPolicyService.BlockedStatus
            : enrollment is { ProgressPercentage: >= 100 }
                ? "Concluido"
                : "Active";

        if (enrollment == null)
        {
            enrollment = new CourseEnrollment
            {
                UserId = payment.UserId,
                CourseId = payment.CourseId,
                Status = nextStatus,
                ProgressPercentage = 0,
                StartDate = DateTime.Now
            };
            _context.CourseEnrollments.Add(enrollment);
        }
        else
        {
            enrollment.Status = nextStatus;
            enrollment.StartDate ??= DateTime.Now;
            if (enrollment.ProgressPercentage < 0)
                enrollment.ProgressPercentage = 0;
        }

        _context.PaymentAudits.Add(new PaymentAudit
        {
            PaymentId = payment.Id,
            Action = auditAction,
            Details = auditDetails
        });
    }
}
