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
    [Route("api/[controller]")]
    public class CoursesEnrollmentController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;
        private readonly IMapper _mapper;

        public CoursesEnrollmentController(EducationalPlataformContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CourseEnrollmentReadDto>> GetAll([FromQuery] int? userId)
        {
            if (!TryGetCurrentUserId(out var currentUserId))
                return Unauthorized();

            var query = _context.CourseEnrollments
                .Include(e => e.User)
                .Include(e => e.Course)
                .AsQueryable();

            if (IsStaff())
            {
                if (userId.HasValue)
                {
                    query = query.Where(e => e.UserId == userId.Value);
                }
            }
            else
            {
                query = query.Where(e => e.UserId == currentUserId);
            }

            var enrollments = query.ToList();
            var enrollmentsDto = _mapper.Map<List<CourseEnrollmentReadDto>>(enrollments);
            return Ok(enrollmentsDto);
        }

        [HttpGet("{id}")]
        public ActionResult<CourseEnrollmentReadDto> GetById(int id)
        {
            var enrollment = _context.CourseEnrollments.Find(id);
            if (enrollment == null)
                throw new ArgumentException($"Course with id {id} not found");

            var enrollmentDto = _mapper.Map<CourseEnrollmentReadDto>(enrollment);
            return Ok(enrollmentDto);
        }

        [HttpGet("teacher/{teacherId}/students-count")]
        public ActionResult<int> GetStudentsCountByTeacher(int teacherId)
        {
            var coursesIds = _context.Courses
                .Where(c => c.CreatorId == teacherId)
                .Select(c => c.Id)
                .ToList();

            var count = _context.CourseEnrollments
                .Count(e => coursesIds.Contains(e.CourseId));

            return Ok(count);
        }


        [HttpPost]
        public async Task<ActionResult<CourseEnrollmentReadDto>> Create(
        [FromBody] CourseEnrollmentCreateDto dto)
        {
            var existingEnrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e =>
                    e.UserId == dto.UserId &&
                    e.CourseId == dto.CourseId);

            if (existingEnrollment != null)
            {
                return Conflict(new
                {
                    message = "O aluno já está matriculado neste curso.",
                    enrollmentId = existingEnrollment.Id,
                    status = existingEnrollment.Status
                });
            }

            var enrollment = _mapper.Map<CourseEnrollment>(dto);

            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            var enrollmentReadDto = _mapper.Map<CourseEnrollmentReadDto>(enrollment);

            return CreatedAtAction(
                nameof(GetById),
                new { id = enrollment.Id },
                enrollmentReadDto);
        }

        [HttpPut("{id}")]
        public ActionResult Update(int id, [FromBody] CourseEnrollmentUpdateDto dto)
        {
            var enrollment = _context.CourseEnrollments.Find(id);
            if (enrollment == null)
                throw new ArgumentException($"Course with id {id} not found");

            _mapper.Map(dto, enrollment);
            _context.SaveChanges();

            return NoContent();
        }


        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            var enrollment = _context.CourseEnrollments.Find(id);
            if (enrollment == null)
                throw new ArgumentException($"Course with id {id} not found");

            _context.CourseEnrollments.Remove(enrollment);
            _context.SaveChanges();
            return NoContent();
        }

        private bool TryGetCurrentUserId(out int userId)
        {
            userId = 0;
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out userId);
        }

        private bool IsStaff()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            return string.Equals(role, nameof(UserProfile.Coordinator), StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, nameof(UserProfile.Teacher), StringComparison.OrdinalIgnoreCase);
        }
    }
}
