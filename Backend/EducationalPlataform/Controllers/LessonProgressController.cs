using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EducationalPlataform.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LessonProgressController : ControllerBase
    {
        private static readonly string[] ActiveEnrollmentStatuses =
        {
            "Active",
            "Ativo",
            "Completed",
            "Concluido",
            "Concluído"
        };

        private readonly EducationalPlataformContext _context;

        public LessonProgressController(EducationalPlataformContext context)
        {
            _context = context;
        }

        [HttpGet("course/{courseId:int}")]
        public async Task<ActionResult<IEnumerable<LessonProgressReadDto>>> GetByCourse(int courseId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            if (!await CanAccessCourseAsync(userId, courseId))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Você não tem acesso a este curso." });

            var progresses = await _context.LessonProgresses
                .AsNoTracking()
                .Where(p => p.UserId == userId && p.Lesson.CourseModule != null && p.Lesson.CourseModule.CourseId == courseId)
                .Select(p => new LessonProgressReadDto
                {
                    LessonId = p.LessonId,
                    Completed = p.Completed,
                    CompletedAt = p.CompletedAt,
                    LastWatchedSecond = p.LastWatchedSecond,
                    MaxWatchedSecond = p.MaxWatchedSecond
                })
                .ToListAsync();

            return Ok(progresses);
        }

        [HttpPut("lessons/{lessonId:int}/watch")]
        public async Task<ActionResult<LessonProgressReadDto>> Watch(
            int lessonId,
            [FromBody] LessonWatchUpdateDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var lesson = await _context.Lessons
                .Include(l => l.CourseModule)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson?.CourseModule == null)
                return NotFound("Aula não encontrada.");

            if (!await CanAccessCourseAsync(userId, lesson.CourseModule.CourseId))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Você não tem acesso a este curso." });

            var progress = await GetOrCreateProgressAsync(userId, lessonId);
            var incoming = Math.Max(0, dto.LastWatchedSecond);
            const int skipGraceSeconds = 12;
            if (incoming <= progress.MaxWatchedSecond + skipGraceSeconds)
            {
                progress.LastWatchedSecond = incoming;
                progress.MaxWatchedSecond = Math.Max(progress.MaxWatchedSecond, incoming);
            }
            else
            {
                progress.LastWatchedSecond = progress.MaxWatchedSecond;
            }
            progress.LastAccessAt = DateTime.UtcNow;

            if (dto.DurationSeconds > 0)
            {
                progress.TotalWatchedSeconds = Math.Max(progress.TotalWatchedSeconds, dto.DurationSeconds);
            }

            await _context.SaveChangesAsync();
            return Ok(ToDto(progress));
        }

        [HttpPut("lessons/{lessonId:int}/complete")]
        public async Task<ActionResult<LessonProgressReadDto>> Complete(int lessonId)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var lesson = await _context.Lessons
                .Include(l => l.CourseModule)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson?.CourseModule == null)
                return NotFound("Aula não encontrada.");

            var courseId = lesson.CourseModule.CourseId;

            if (!await CanAccessCourseAsync(userId, courseId))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Você não tem acesso a este curso." });

            var progress = await GetOrCreateProgressAsync(userId, lessonId);
            var duration = lesson.DurationSeconds > 0 ? lesson.DurationSeconds : progress.TotalWatchedSeconds;
            var required = duration > 0 ? duration * 0.95 : double.MaxValue;

            if (!progress.Completed && progress.MaxWatchedSecond < required)
            {
                return BadRequest(new { message = "Assista pelo menos 95% da aula para marcá-la como concluída." });
            }

            progress.Completed = true;
            progress.CompletedAt ??= DateTime.UtcNow;
            progress.LastAccessAt = DateTime.UtcNow;
            progress.ViewCount += 1;

            await _context.SaveChangesAsync();
            await RecalculateEnrollmentAsync(userId, courseId);
            await _context.SaveChangesAsync();

            return Ok(ToDto(progress));
        }

        private async Task<LessonProgress> GetOrCreateProgressAsync(int userId, int lessonId)
        {
            var progress = await _context.LessonProgresses
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LessonId == lessonId);

            if (progress != null)
                return progress;

            progress = new LessonProgress(userId, lessonId)
            {
                LastAccessAt = DateTime.UtcNow
            };
            _context.LessonProgresses.Add(progress);
            return progress;
        }

        private static LessonProgressReadDto ToDto(LessonProgress progress) => new()
        {
            LessonId = progress.LessonId,
            Completed = progress.Completed,
            CompletedAt = progress.CompletedAt,
            LastWatchedSecond = progress.LastWatchedSecond,
            MaxWatchedSecond = progress.MaxWatchedSecond
        };

        private async Task RecalculateEnrollmentAsync(int userId, int courseId)
        {
            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

            if (enrollment == null)
                return;

            var publishedLessonIds = await _context.Lessons
                .Where(l => l.CourseModule != null && l.CourseModule.CourseId == courseId && l.IsPublished)
                .Select(l => l.Id)
                .ToListAsync();

            var completed = await _context.LessonProgresses
                .CountAsync(p =>
                    p.UserId == userId &&
                    p.Completed &&
                    publishedLessonIds.Contains(p.LessonId));

            enrollment.TotalLessons = publishedLessonIds.Count;
            enrollment.CompletedLessons = completed;
            enrollment.ProgressPercentage = publishedLessonIds.Count == 0
                ? 0
                : (int)Math.Round(100.0 * completed / publishedLessonIds.Count);

            if (enrollment.ProgressPercentage >= 100 && ActiveEnrollmentStatuses.Contains(enrollment.Status))
            {
                enrollment.Status = "Concluido";
            }
        }

        private async Task<bool> CanAccessCourseAsync(int userId, int courseId)
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (string.Equals(role, nameof(UserProfile.Coordinator), StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, nameof(UserProfile.Teacher), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return await _context.CourseEnrollments.AnyAsync(e =>
                e.UserId == userId &&
                e.CourseId == courseId &&
                ActiveEnrollmentStatuses.Contains(e.Status));
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out userId);
        }
    }
}
