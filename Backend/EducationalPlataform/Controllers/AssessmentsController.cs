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
    public class AssessmentsController : ControllerBase
    {
        public const int MaxExamAttempts = 3;
        private const decimal ActivityCompletionRatio = 0.9m;

        private static readonly string[] ActiveEnrollmentStatuses =
        {
            "Active", "Ativo", "Completed", "Concluido", "Concluído"
        };

        private readonly EducationalPlataformContext _context;

        public AssessmentsController(EducationalPlataformContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Coordinator,Teacher")]
        public async Task<IActionResult> ListStaff()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var query = _context.Assessments
                .AsNoTracking()
                .Include(a => a.Course)
                .Include(a => a.Questions)
                .AsQueryable();

            if (IsTeacher())
                query = query.Where(a => a.Course.TeacherId == userId);

            var items = await query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.CourseId,
                    CourseTitle = a.Course.Title,
                    a.Title,
                    a.Description,
                    Type = a.Type.ToString(),
                    a.PassingScore,
                    a.IsPublished,
                    QuestionsCount = a.Questions.Count,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("mine")]
        public async Task<IActionResult> ListMine()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var courseIds = await _context.CourseEnrollments
                .Where(e => e.UserId == userId && ActiveEnrollmentStatuses.Contains(e.Status))
                .Select(e => e.CourseId)
                .ToListAsync();

            var assessments = await _context.Assessments
                .AsNoTracking()
                .Include(a => a.Course)
                .Where(a => a.IsPublished && courseIds.Contains(a.CourseId))
                .OrderBy(a => a.Course.Title)
                .ThenBy(a => a.Title)
                .ToListAsync();

            var ids = assessments.Select(a => a.Id).ToList();
            var attempts = await _context.AssessmentAttempts
                .AsNoTracking()
                .Where(t => t.UserId == userId && ids.Contains(t.AssessmentId))
                .ToListAsync();

            var result = new List<object>();
            foreach (var assessment in assessments)
            {
                var mine = attempts.Where(t => t.AssessmentId == assessment.Id).ToList();
                var submitted = mine.Where(t => t.Status == "Submitted").ToList();
                var access = assessment.Type == AssessmentType.Exam
                    ? await GetExamAccessAsync(userId, assessment, mine)
                    : null;

                result.Add(new
                {
                    assessment.Id,
                    assessment.CourseId,
                    CourseTitle = assessment.Course.Title,
                    assessment.Title,
                    assessment.Description,
                    Type = assessment.Type.ToString(),
                    assessment.PassingScore,
                    BestScore = submitted.Count == 0 ? (decimal?)null : submitted.Max(t => t.Score),
                    AttemptsCount = mine.Count,
                    MaxAttempts = assessment.Type == AssessmentType.Exam ? MaxExamAttempts : (int?)null,
                    CanStart = access?.CanStart ?? true,
                    BlockReason = access?.Reason,
                    ActivitiesCompleted = access?.ActivitiesCompleted,
                    ActivitiesTotal = access?.ActivitiesTotal,
                    InProgress = mine.Any(t => t.Status == "InProgress")
                });
            }

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetForStudent(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var assessment = await LoadAssessmentAsync(id);
            if (assessment == null)
                return NotFound();

            if (!assessment.IsPublished && !IsStaff())
                return NotFound();

            if (!await CanAccessCourseAsync(userId, assessment.CourseId))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sem acesso a este curso." });

            var mine = await _context.AssessmentAttempts
                .Where(t => t.UserId == userId && t.AssessmentId == id)
                .ToListAsync();

            var access = assessment.Type == AssessmentType.Exam
                ? await GetExamAccessAsync(userId, assessment, mine)
                : new ExamAccess(true, null, 0, 0);

            var includeQuestions = assessment.Type != AssessmentType.Exam || IsStaff();

            return Ok(new
            {
                assessment.Id,
                assessment.CourseId,
                CourseTitle = assessment.Course.Title,
                assessment.Title,
                assessment.Description,
                Type = assessment.Type.ToString(),
                assessment.PassingScore,
                AttemptsCount = mine.Count,
                MaxAttempts = assessment.Type == AssessmentType.Exam ? MaxExamAttempts : (int?)null,
                access.CanStart,
                BlockReason = access.Reason,
                ActivitiesCompleted = access.ActivitiesCompleted,
                ActivitiesTotal = access.ActivitiesTotal,
                InProgressAttemptId = mine.FirstOrDefault(t => t.Status == "InProgress")?.Id,
                Questions = includeQuestions ? MapQuestions(assessment) : Array.Empty<object>()
            });
        }

        [HttpPost]
        [Authorize(Roles = "Coordinator,Teacher")]
        public async Task<IActionResult> Create([FromBody] AssessmentCreateDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Questions.Count == 0)
                return BadRequest(new { message = "Informe título e pelo menos uma pergunta." });

            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == dto.CourseId);
            if (course == null)
                return NotFound(new { message = "Curso não encontrado." });

            if (!CanManageCourse(userId, course))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Você só pode criar atividades e provas dos seus cursos." });

            foreach (var question in dto.Questions)
            {
                if (question.Options.Count(o => o.IsCorrect) != 1)
                    return BadRequest(new { message = "Cada pergunta precisa ter exatamente uma alternativa correta." });
            }

            var assessment = new Assessment
            {
                CourseId = dto.CourseId,
                Title = dto.Title.Trim(),
                Description = dto.Description,
                Type = dto.Type == AssessmentType.Exam ? AssessmentType.Exam : AssessmentType.Activity,
                PassingScore = dto.PassingScore > 0 ? dto.PassingScore : 70,
                IsPublished = dto.IsPublished,
                CreatedByUserId = userId,
                CreatedAt = DateTime.Now
            };

            AddQuestions(assessment, dto.Questions);

            _context.Assessments.Add(assessment);
            await _context.SaveChangesAsync();
            return Ok(new { assessment.Id });
        }

        [HttpGet("{id:int}/manage")]
        [Authorize(Roles = "Coordinator,Teacher")]
        public async Task<IActionResult> GetForStaff(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var assessment = await LoadAssessmentAsync(id);
            if (assessment == null)
                return NotFound();

            if (!CanManageCourse(userId, assessment.Course))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sem permissão para gerir esta avaliação." });

            return Ok(new
            {
                assessment.Id,
                assessment.CourseId,
                assessment.Title,
                assessment.Description,
                Type = assessment.Type == AssessmentType.Exam ? 2 : 1,
                assessment.PassingScore,
                assessment.IsPublished,
                Questions = assessment.Questions.Select(q => new
                {
                    q.Id,
                    q.Prompt,
                    q.Points,
                    Options = q.Options.Select(o => new
                    {
                        o.Id,
                        o.Text,
                        o.IsCorrect
                    })
                })
            });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Coordinator,Teacher")]
        public async Task<IActionResult> Update(int id, [FromBody] AssessmentCreateDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Questions.Count == 0)
                return BadRequest(new { message = "Informe título e pelo menos uma pergunta." });

            var assessment = await LoadAssessmentAsync(id);
            if (assessment == null)
                return NotFound();

            if (!CanManageCourse(userId, assessment.Course))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sem permissão para editar esta avaliação." });

            foreach (var question in dto.Questions)
            {
                if (question.Options.Count(o => o.IsCorrect) != 1)
                    return BadRequest(new { message = "Cada pergunta precisa ter exatamente uma alternativa correta." });
            }

            assessment.Title = dto.Title.Trim();
            assessment.Description = dto.Description;
            assessment.Type = dto.Type == AssessmentType.Exam ? AssessmentType.Exam : AssessmentType.Activity;
            assessment.PassingScore = dto.PassingScore > 0 ? dto.PassingScore : 70;
            assessment.IsPublished = dto.IsPublished;

            _context.AssessmentQuestions.RemoveRange(assessment.Questions);
            assessment.Questions.Clear();
            AddQuestions(assessment, dto.Questions);

            await _context.SaveChangesAsync();
            return Ok(new { assessment.Id });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Coordinator,Teacher")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var assessment = await LoadAssessmentAsync(id);
            if (assessment == null)
                return NotFound();

            if (!CanManageCourse(userId, assessment.Course))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sem permissão para excluir esta avaliação." });

            _context.Assessments.Remove(assessment);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/start")]
        public async Task<IActionResult> Start(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var assessment = await LoadAssessmentAsync(id);
            if (assessment is not { IsPublished: true })
                return NotFound();

            if (!await CanAccessCourseAsync(userId, assessment.CourseId))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sem acesso a este curso." });

            var mine = await _context.AssessmentAttempts
                .Where(t => t.UserId == userId && t.AssessmentId == id)
                .ToListAsync();

            var inProgress = mine.FirstOrDefault(t => t.Status == "InProgress");
            if (inProgress != null)
            {
                return Ok(new
                {
                    AttemptId = inProgress.Id,
                    Resumed = true,
                    Questions = MapQuestions(assessment)
                });
            }

            if (assessment.Type == AssessmentType.Exam)
            {
                var access = await GetExamAccessAsync(userId, assessment, mine);
                if (!access.CanStart)
                    return BadRequest(new { message = access.Reason });
            }

            var attempt = new AssessmentAttempt
            {
                AssessmentId = assessment.Id,
                UserId = userId,
                Score = 0,
                Status = "InProgress",
                StartedAt = DateTime.Now,
                SubmittedAt = DateTime.Now
            };
            _context.AssessmentAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                AttemptId = attempt.Id,
                Resumed = false,
                Questions = MapQuestions(assessment)
            });
        }

        [HttpPost("{id:int}/abandon")]
        public async Task<IActionResult> Abandon(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var attempt = await _context.AssessmentAttempts
                .Where(t => t.AssessmentId == id && t.UserId == userId && t.Status == "InProgress")
                .OrderByDescending(t => t.Id)
                .FirstOrDefaultAsync();

            if (attempt == null)
                return Ok(new { abandoned = false });

            attempt.Status = "Abandoned";
            attempt.Score = 0;
            attempt.SubmittedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { abandoned = true, attempt.Id, Score = 0 });
        }

        [HttpPost("{id:int}/submit")]
        public async Task<IActionResult> Submit(int id, [FromBody] AssessmentSubmitDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var assessment = await LoadAssessmentAsync(id);
            if (assessment is not { IsPublished: true })
                return NotFound();

            if (!await CanAccessCourseAsync(userId, assessment.CourseId))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Sem acesso a este curso." });

            AssessmentAttempt attempt;
            if (assessment.Type == AssessmentType.Exam)
            {
                var inProgress = await _context.AssessmentAttempts
                    .Include(t => t.Answers)
                    .FirstOrDefaultAsync(t =>
                        t.AssessmentId == id &&
                        t.UserId == userId &&
                        t.Status == "InProgress");

                if (inProgress == null)
                    return BadRequest(new { message = "Inicie a prova antes de enviar as respostas." });

                attempt = inProgress;
            }
            else
            {
                attempt = new AssessmentAttempt
                {
                    AssessmentId = assessment.Id,
                    UserId = userId,
                    StartedAt = DateTime.Now
                };
                _context.AssessmentAttempts.Add(attempt);
            }

            decimal earned = 0;
            decimal total = assessment.Questions.Sum(q => q.Points);
            attempt.Answers.Clear();

            foreach (var question in assessment.Questions)
            {
                var selected = dto.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                if (selected == null)
                    continue;
                var option = question.Options.FirstOrDefault(o => o.Id == selected.OptionId);
                if (option == null)
                    continue;
                attempt.Answers.Add(new AssessmentAnswer
                {
                    QuestionId = question.Id,
                    SelectedOptionId = option.Id
                });
                if (option.IsCorrect)
                    earned += question.Points;
            }

            var score = total == 0 ? 0 : Math.Round(100 * earned / total, 2);
            attempt.Score = score;
            attempt.Status = "Submitted";
            attempt.SubmittedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                attempt.Id,
                Score = score,
                Passed = score >= assessment.PassingScore,
                assessment.PassingScore
            });
        }

        private async Task<Assessment?> LoadAssessmentAsync(int id) =>
            await _context.Assessments
                .Include(a => a.Course)
                .Include(a => a.Questions.OrderBy(q => q.Order))
                    .ThenInclude(q => q.Options.OrderBy(o => o.Order))
                .FirstOrDefaultAsync(a => a.Id == id);

        private static void AddQuestions(Assessment assessment, List<AssessmentQuestionWriteDto> questions)
        {
            var qOrder = 1;
            foreach (var question in questions)
            {
                var entity = new AssessmentQuestion
                {
                    Prompt = question.Prompt.Trim(),
                    Order = qOrder++,
                    Points = question.Points > 0 ? question.Points : 1
                };
                var oOrder = 1;
                foreach (var option in question.Options.Where(o => !string.IsNullOrWhiteSpace(o.Text)))
                {
                    entity.Options.Add(new AssessmentOption
                    {
                        Text = option.Text.Trim(),
                        IsCorrect = option.IsCorrect,
                        Order = oOrder++
                    });
                }
                assessment.Questions.Add(entity);
            }
        }

        private static object MapQuestions(Assessment assessment) =>
            assessment.Questions.Select(q => new
            {
                q.Id,
                q.Prompt,
                q.Points,
                Options = q.Options.Select(o => new { o.Id, o.Text })
            });

        private async Task<ExamAccess> GetExamAccessAsync(
            int userId,
            Assessment assessment,
            List<AssessmentAttempt> mine)
        {
            if (mine.Any(t => t.Status == "InProgress"))
                return new ExamAccess(true, null, 0, 0);

            if (mine.Count >= MaxExamAttempts)
                return new ExamAccess(false, "Já utilizaste as 3 tentativas desta prova.", 0, 0);

            var activities = await _context.Assessments
                .AsNoTracking()
                .Where(a => a.CourseId == assessment.CourseId && a.IsPublished && a.Type == AssessmentType.Activity)
                .Select(a => new { a.Id, a.PassingScore })
                .ToListAsync();

            if (activities.Count == 0)
                return new ExamAccess(true, null, 0, 0);

            var activityIds = activities.Select(a => a.Id).ToList();
            var activityAttempts = await _context.AssessmentAttempts
                .AsNoTracking()
                .Where(t => t.UserId == userId && activityIds.Contains(t.AssessmentId) && t.Status == "Submitted")
                .ToListAsync();

            var completed = activities.Count(activity =>
            {
                var scores = activityAttempts.Where(t => t.AssessmentId == activity.Id).Select(t => t.Score).ToList();
                return scores.Count > 0 && scores.Max() >= activity.PassingScore;
            });

            var required = (int)Math.Ceiling(activities.Count * (double)ActivityCompletionRatio);
            if (completed < required)
            {
                return new ExamAccess(
                    false,
                    $"Conclui pelo menos 90% das atividades (com 70% ou mais) antes da prova. Progresso: {completed}/{activities.Count}.",
                    completed,
                    activities.Count);
            }

            return new ExamAccess(true, null, completed, activities.Count);
        }

        private sealed record ExamAccess(bool CanStart, string? Reason, int ActivitiesCompleted, int ActivitiesTotal);

        private bool CanManageCourse(int userId, Course course)
        {
            if (IsCoordinator())
                return true;
            return IsTeacher() && course.TeacherId == userId;
        }

        private async Task<bool> CanAccessCourseAsync(int userId, int courseId)
        {
            if (IsStaff())
            {
                if (IsCoordinator())
                    return true;
                return await _context.Courses.AnyAsync(c => c.Id == courseId && c.TeacherId == userId);
            }

            return await _context.CourseEnrollments.AnyAsync(e =>
                e.UserId == userId &&
                e.CourseId == courseId &&
                ActiveEnrollmentStatuses.Contains(e.Status));
        }

        private bool IsStaff() => IsCoordinator() || IsTeacher();
        private bool IsCoordinator() => string.Equals(User.FindFirstValue(ClaimTypes.Role), nameof(UserProfile.Coordinator), StringComparison.OrdinalIgnoreCase);
        private bool IsTeacher() => string.Equals(User.FindFirstValue(ClaimTypes.Role), nameof(UserProfile.Teacher), StringComparison.OrdinalIgnoreCase);

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out userId);
        }
    }
}
