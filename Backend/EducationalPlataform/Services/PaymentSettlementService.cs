using EducationalPlataform.Data;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Services;

public sealed class PaymentSettlementService
{
    private readonly EducationalPlataformContext _context;

    public PaymentSettlementService(EducationalPlataformContext context)
    {
        _context = context;
    }

    public async Task ApplyPaidAndUnlockAsync(
        Payment payment,
        DateTime paidAt,
        string auditAction,
        string auditDetails)
    {
        payment.Status = PaymentStatus.Paid;
        payment.PaidAt = paidAt;
        payment.SettledAt = DateTime.Now;

        var enrollment = await _context.CourseEnrollments
            .FirstOrDefaultAsync(e =>
                e.UserId == payment.UserId &&
                e.CourseId == payment.CourseId);

        if (enrollment == null)
        {
            enrollment = new CourseEnrollment
            {
                UserId = payment.UserId,
                CourseId = payment.CourseId,
                Status = "Active",
                ProgressPercentage = 0,
                StartDate = DateTime.Now
            };
            _context.CourseEnrollments.Add(enrollment);
        }
        else
        {
            enrollment.Status = "Active";
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
