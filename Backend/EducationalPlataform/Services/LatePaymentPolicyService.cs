using EducationalPlataform.Data;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Services;

public sealed class LatePaymentPolicyService
{
    public const decimal LateFeeAmount = 10m;
    public const int BlockAfterDays = 30;
    public const string BlockedStatus = "Blocked";
    public const string AccessBlockedMessage =
        "Acesso bloqueado: há mensalidade com mais de 30 dias de atraso. Regularize no financeiro para liberar o curso.";

    public static readonly string[] AccessibleStatuses =
    {
        "Active",
        "Ativo",
        "Completed",
        "Concluido",
        "Concluído"
    };

    private readonly EducationalPlataformContext _context;

    public LatePaymentPolicyService(EducationalPlataformContext context)
    {
        _context = context;
    }

    public Task ApplyForUserAsync(int userId, CancellationToken cancellationToken = default)
        => ApplyAsync(userId, null, cancellationToken);

    public Task ApplyForUserCourseAsync(int userId, int courseId, CancellationToken cancellationToken = default)
        => ApplyAsync(userId, courseId, cancellationToken);

    public async Task ApplyAllPendingAsync(CancellationToken cancellationToken = default)
        => await ApplyAsync(null, null, cancellationToken);

    public void EnsureLateFee(Payment payment, DateTime paidAt)
    {
        if (payment.LateFeeApplied || payment.Status == PaymentStatus.Cancelled)
            return;

        if (!payment.DueDate.HasValue)
            return;

        if (paidAt.Date <= payment.DueDate.Value.Date)
            return;

        ApplyFee(payment);
    }

    public static bool IsBlockingOverdue(Payment payment, DateTime today)
        => payment.Status == PaymentStatus.Pending
           && payment.DueDate.HasValue
           && today > payment.DueDate.Value.Date.AddDays(BlockAfterDays);

    public async Task ApplyAsync(int? userId, int? courseId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var paymentsQuery = _context.Payments.Where(p => p.Status == PaymentStatus.Pending);
        if (userId.HasValue)
            paymentsQuery = paymentsQuery.Where(p => p.UserId == userId.Value);
        if (courseId.HasValue)
            paymentsQuery = paymentsQuery.Where(p => p.CourseId == courseId.Value);

        var payments = await paymentsQuery.ToListAsync(cancellationToken);
        var changed = false;

        foreach (var payment in payments)
        {
            if (!payment.DueDate.HasValue || payment.LateFeeApplied)
                continue;

            if (payment.DueDate.Value.Date >= today)
                continue;

            ApplyFee(payment);
            _context.PaymentAudits.Add(new PaymentAudit
            {
                PaymentId = payment.Id,
                Action = "LateFeeApplied",
                Details = $"Multa de atraso de {LateFeeAmount:0.00} aplicada uma vez."
            });
            changed = true;
        }

        var enrollmentsQuery = _context.CourseEnrollments.AsQueryable();
        if (userId.HasValue)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.UserId == userId.Value);
        if (courseId.HasValue)
            enrollmentsQuery = enrollmentsQuery.Where(e => e.CourseId == courseId.Value);
        else if (!userId.HasValue)
        {
            var userIds = payments.Select(p => p.UserId).Distinct().ToList();
            enrollmentsQuery = enrollmentsQuery.Where(e =>
                e.Status == BlockedStatus || userIds.Contains(e.UserId));
        }

        var enrollments = await enrollmentsQuery.ToListAsync(cancellationToken);
        if (enrollments.Count == 0 && !changed)
            return;

        foreach (var enrollment in enrollments)
        {
            if (IsPendingStatus(enrollment.Status))
                continue;

            var blocked = payments.Any(p =>
                p.UserId == enrollment.UserId
                && p.CourseId == enrollment.CourseId
                && IsBlockingOverdue(p, today));

            if (blocked)
            {
                if (!string.Equals(enrollment.Status, BlockedStatus, StringComparison.OrdinalIgnoreCase))
                {
                    enrollment.Status = BlockedStatus;
                    changed = true;
                }
            }
            else if (string.Equals(enrollment.Status, BlockedStatus, StringComparison.OrdinalIgnoreCase))
            {
                enrollment.Status = enrollment.ProgressPercentage >= 100 ? "Concluido" : "Active";
                changed = true;
            }
        }

        if (changed)
            await _context.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyFee(Payment payment)
    {
        payment.Amount += LateFeeAmount;
        payment.LateFeeApplied = true;
        payment.PixCopyPaste = null;
        payment.PixInvoiceId = null;
        payment.PixTransactionId = null;
        payment.PixExpiresAt = null;
    }

    private static bool IsPendingStatus(string status)
        => string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase)
           || string.Equals(status, "Pendente", StringComparison.OrdinalIgnoreCase);
}
