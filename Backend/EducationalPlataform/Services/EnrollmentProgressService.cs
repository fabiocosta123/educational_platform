using EducationalPlataform.Data;
using EducationalPlataform.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Services;

public class EnrollmentProgressService
{
    private static readonly string[] CompletedStatuses =
    {
        "Completed",
        "Concluido",
        "Concluído"
    };

    private static readonly string[] ActiveStatuses =
    {
        "Active",
        "Ativo",
        "Completed",
        "Concluido",
        "Concluído"
    };

    private readonly EducationalPlataformContext _context;

    public EnrollmentProgressService(EducationalPlataformContext context)
    {
        _context = context;
    }

    public async Task RecalculateCourseAsync(int courseId)
    {
        var publishedLessonIds = await _context.Lessons
            .Where(l => l.CourseModule != null && l.CourseModule.CourseId == courseId && l.IsPublished)
            .Select(l => l.Id)
            .ToListAsync();

        var enrollments = await _context.CourseEnrollments
            .Where(e => e.CourseId == courseId)
            .ToListAsync();

        var completedByUser = await _context.LessonProgresses
            .Where(p => p.Completed && publishedLessonIds.Contains(p.LessonId))
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        foreach (var enrollment in enrollments)
        {
            var completed = completedByUser.GetValueOrDefault(enrollment.UserId);
            enrollment.TotalLessons = publishedLessonIds.Count;
            enrollment.CompletedLessons = completed;
            enrollment.ProgressPercentage = publishedLessonIds.Count == 0
                ? 0
                : (int)Math.Round(100.0 * completed / publishedLessonIds.Count);

            if (enrollment.ProgressPercentage >= 100 && ActiveStatuses.Contains(enrollment.Status))
            {
                enrollment.Status = "Concluido";
            }
            else if (CompletedStatuses.Contains(enrollment.Status) && enrollment.ProgressPercentage < 100
                     && enrollment.Status != "Pending" && enrollment.Status != "Pendente"
                     && enrollment.Status != "Cancelled" && enrollment.Status != "Cancelado")
            {
                enrollment.Status = "Ativo";
            }
        }
    }
}
