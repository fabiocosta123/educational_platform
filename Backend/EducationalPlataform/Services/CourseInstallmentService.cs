using EducationalPlataform.Data;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Services;

public sealed class CourseInstallmentService
{
    private readonly EducationalPlataformContext _context;

    public CourseInstallmentService(EducationalPlataformContext context)
    {
        _context = context;
    }

    public static int NormalizeInstallmentCount(int count)
        => count is < 1 or > 24 ? 12 : count;

    public static IReadOnlyList<decimal> SplitAmount(decimal total, int count)
    {
        count = NormalizeInstallmentCount(count);
        var cents = (int)Math.Round(total * 100m, MidpointRounding.AwayFromZero);
        if (cents <= 0)
            return Array.Empty<decimal>();

        var baseCents = cents / count;
        var remainder = cents % count;
        var amounts = new decimal[count];
        for (var i = 0; i < count; i++)
        {
            var extra = i == count - 1 ? remainder : 0;
            amounts[i] = (baseCents + extra) / 100m;
        }

        return amounts;
    }

    public async Task EnsureForEnrollmentAsync(int userId, int courseId, DateTime? purchaseDate = null)
    {
        var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null || course.Price <= 0)
            return;

        var alreadyHasPlan = await _context.Payments.AnyAsync(p =>
            p.UserId == userId &&
            p.CourseId == courseId &&
            p.Status != PaymentStatus.Cancelled);

        if (alreadyHasPlan)
            return;

        var count = NormalizeInstallmentCount(course.InstallmentCount);
        var amounts = SplitAmount(course.Price, count);
        if (amounts.Count == 0)
            return;

        var start = (purchaseDate ?? DateTime.Now).Date;
        for (var i = 0; i < amounts.Count; i++)
        {
            _context.Payments.Add(new Payment
            {
                UserId = userId,
                CourseId = courseId,
                Amount = amounts[i],
                Status = PaymentStatus.Pending,
                DueDate = start.AddDays(30 * i),
                InstallmentNumber = i + 1,
                CreatedAt = DateTime.Now
            });
        }
    }
}
