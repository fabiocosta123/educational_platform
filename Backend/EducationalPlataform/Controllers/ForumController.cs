using AutoMapper;
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
    [Route("api/forum")]
    public class ForumController : ControllerBase
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
        private readonly IMapper _mapper;

        public ForumController(EducationalPlataformContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet("~/api/courses/{courseId:int}/forum/questions")]
        public async Task<ActionResult<IEnumerable<ForumQuestionListDto>>> GetByCourse(
            int courseId,
            [FromQuery] int? lessonId,
            [FromQuery] bool? resolved)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound("Curso não encontrado.");

            if (!await CanAccessCourseForumAsync(userId, course))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Você não tem acesso ao fórum deste curso." });

            var questions = await _context.ForumQuestions
                .AsNoTracking()
                .Where(q => q.CourseId == courseId)
                .Where(q => !lessonId.HasValue || q.LessonId == lessonId)
                .Where(q => !resolved.HasValue || q.IsResolved == resolved)
                .OrderByDescending(q => q.CreatedAt)
                .Select(q => new ForumQuestionListDto
                {
                    Id = q.Id,
                    Title = q.Title,
                    CreatedAt = q.CreatedAt,
                    IsResolved = q.IsResolved,
                    ReplyCount = q.Replies.Count,
                    CourseId = q.CourseId,
                    LessonId = q.LessonId,
                    LessonTitle = q.Lesson != null ? q.Lesson.Title : null,
                    UserId = q.UserId,
                    UserName = q.User.UserName ?? string.Empty
                })
                .ToListAsync();

            return Ok(questions);
        }

        [HttpGet("questions/{id:int}")]
        public async Task<ActionResult<ForumQuestionReadDto>> GetById(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var question = await GetQuestionForReadAsync(id);

            if (question == null)
                return NotFound("Pergunta não encontrada.");

            if (!await CanAccessCourseForumAsync(userId, question.Course))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Você não tem acesso ao fórum deste curso." });

            return Ok(_mapper.Map<ForumQuestionReadDto>(question));
        }

        [HttpPost("questions")]
        public async Task<ActionResult<ForumQuestionReadDto>> CreateQuestion([FromBody] ForumQuestionCreateDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == dto.CourseId);

            if (course == null)
                return BadRequest("Curso não encontrado.");

            if (!await CanAccessCourseForumAsync(userId, course))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Você não tem acesso ao fórum deste curso." });

            var lessonError = await ValidateLessonBelongsToCourseAsync(dto.LessonId, dto.CourseId);
            if (lessonError != null)
                return BadRequest(lessonError);

            var question = _mapper.Map<ForumQuestion>(dto);
            question.UserId = userId;
            question.CreatedAt = DateTime.UtcNow;
            question.IsResolved = false;

            _context.ForumQuestions.Add(question);
            await _context.SaveChangesAsync();

            var created = await GetQuestionForReadAsync(question.Id);
            if (created == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "A pergunta foi criada, mas não pôde ser recarregada." });

            return CreatedAtAction(
                nameof(GetById),
                new { id = question.Id },
                _mapper.Map<ForumQuestionReadDto>(created));
        }

        [HttpPut("questions/{id:int}")]
        public async Task<ActionResult<ForumQuestionReadDto>> UpdateQuestion(int id, [FromBody] ForumQuestionUpdateDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var question = await _context.ForumQuestions
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return NotFound("Pergunta não encontrada.");

            if (!IsCoordinator() && question.UserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Somente o autor pode editar esta pergunta." });

            question.Title = dto.Title;
            question.Content = dto.Content;
            question.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var updated = await GetQuestionForReadAsync(question.Id);
            return Ok(_mapper.Map<ForumQuestionReadDto>(updated));
        }

        [HttpPatch("questions/{id:int}/resolve")]
        public async Task<ActionResult<ForumQuestionReadDto>> ResolveQuestion(int id, [FromBody] ForumQuestionResolveDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var question = await _context.ForumQuestions
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return NotFound("Pergunta não encontrada.");

            if (!IsAuthorOrCourseStaff(userId, question))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Somente o autor, o professor do curso ou o coordenador pode alterar o status da pergunta." });

            question.IsResolved = dto.IsResolved;
            question.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var updated = await GetQuestionForReadAsync(question.Id);
            return Ok(_mapper.Map<ForumQuestionReadDto>(updated));
        }

        [HttpPost("questions/{id:int}/replies")]
        public async Task<ActionResult<ForumReplyReadDto>> CreateReply(int id, [FromBody] ForumReplyCreateDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var question = await _context.ForumQuestions
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return NotFound("Pergunta não encontrada.");

            if (!await CanAccessCourseForumAsync(userId, question.Course))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Você não tem acesso ao fórum deste curso." });

            var reply = _mapper.Map<ForumReply>(dto);
            reply.ForumQuestionId = id;
            reply.UserId = userId;
            reply.CreatedAt = DateTime.UtcNow;

            _context.ForumReplies.Add(reply);
            await _context.SaveChangesAsync();

            await _context.Entry(reply).Reference(r => r.User).LoadAsync();

            return Created(
                $"/api/forum/questions/{id}",
                _mapper.Map<ForumReplyReadDto>(reply));
        }

        [HttpPut("replies/{id:int}")]
        public async Task<ActionResult<ForumReplyReadDto>> UpdateReply(int id, [FromBody] ForumReplyUpdateDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var reply = await _context.ForumReplies
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reply == null)
                return NotFound("Resposta não encontrada.");

            if (!IsCoordinator() && reply.UserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Somente o autor pode editar esta resposta." });

            reply.Content = dto.Content;
            reply.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(_mapper.Map<ForumReplyReadDto>(reply));
        }

        [HttpDelete("questions/{id:int}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var question = await _context.ForumQuestions
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return NotFound("Pergunta não encontrada.");

            if (!IsAuthorOrCourseStaff(userId, question))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Somente o autor, o professor do curso ou o coordenador pode excluir esta pergunta." });

            _context.ForumQuestions.Remove(question);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("replies/{id:int}")]
        public async Task<IActionResult> DeleteReply(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized();

            var reply = await _context.ForumReplies
                .Include(r => r.ForumQuestion)
                    .ThenInclude(q => q.Course)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reply == null)
                return NotFound("Resposta não encontrada.");

            var isAuthor = reply.UserId == userId;
            var canModerate = IsAuthorOrCourseStaff(userId, reply.ForumQuestion);

            if (!isAuthor && !canModerate)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Somente o autor, o professor do curso ou o coordenador pode excluir esta resposta." });

            _context.ForumReplies.Remove(reply);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task<ForumQuestion?> GetQuestionForReadAsync(int id)
        {
            var question = await _context.ForumQuestions
                .AsNoTracking()
                .Include(q => q.User)
                .Include(q => q.Course)
                .Include(q => q.Lesson)
                .Include(q => q.Replies)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question != null)
            {
                question.Replies = question.Replies
                    .OrderBy(r => r.CreatedAt)
                    .ToList();
            }

            return question;
        }

        private async Task<string?> ValidateLessonBelongsToCourseAsync(int? lessonId, int courseId)
        {
            if (!lessonId.HasValue)
                return null;

            var belongsToCourse = await _context.Lessons
                .AnyAsync(l =>
                    l.Id == lessonId &&
                    l.CourseModule != null &&
                    l.CourseModule.CourseId == courseId);

            return belongsToCourse
                ? null
                : "A aula não pertence a este curso.";
        }

        private async Task<bool> CanAccessCourseForumAsync(int userId, Course course)
        {
            if (IsCoordinator())
                return true;

            if (course.TeacherId == userId || course.CreatorId == userId)
                return true;

            return await _context.CourseEnrollments.AnyAsync(e =>
                e.UserId == userId &&
                e.CourseId == course.Id &&
                ActiveEnrollmentStatuses.Contains(e.Status));
        }

        private bool IsAuthorOrCourseStaff(int userId, ForumQuestion question)
        {
            if (IsCoordinator())
                return true;

            if (question.UserId == userId)
                return true;

            return question.Course.TeacherId == userId || question.Course.CreatorId == userId;
        }

        private bool IsCoordinator()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return string.Equals(role, nameof(UserProfile.Coordinator), StringComparison.OrdinalIgnoreCase);
        }

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out userId);
        }
    }
}
