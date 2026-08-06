using AutoMapper;
using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Controllers
{
    [Authorize(Roles = "Coordinator,Teacher")]
    [ApiController]
    [Route("api/[controller]s")]
    public class TeacherController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;
        private readonly IMapper _mapper;

        public TeacherController(EducationalPlataformContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet("{teacherId}/dashboard")]
        public async Task<ActionResult<object>> GetDashboard(int teacherId)
        {
            var courses = await _context.Courses
                .Where(c => c.TeacherId == teacherId)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .Include(c => c.EnrolledUsers)
                .ToListAsync();

            var coursesCount = courses.Count;

            var lessonsCount = courses.Sum(c => c.Modules.Sum(m => m.Lessons.Count));

            var studentsCount = courses.Sum(c => c.EnrolledUsers.Count);

            var nextLesson = (Lesson?)null;

            return Ok(new
            {
                coursesCount,
                lessonsCount,
                studentsCount,
                nextLessonDate = (DateTime?)null
            });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeacherReadDto>>> GetTeachers()
        {
            try
            {
                var teachers = await _context.Users
                    .Where(u => u.Profile == UserProfile.Teacher)
                    .Include(u => u.CoursesTaught)
                        .ThenInclude(c => c.Modules)
                        .ThenInclude(m => m.Lessons)

                    .Include(u => u.CoursesTaught)
                        .ThenInclude(c => c.EnrolledUsers)
                            .ThenInclude(e => e.User)
                    .ToListAsync();

                var teachersDto = _mapper.Map<List<TeacherReadDto>>(teachers);
                return Ok(teachersDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> GetTeachersList()
        {
            var teachers = await _context.Users
                .Where(u => u.Profile == UserProfile.Teacher)
                .ToListAsync();

            var teachersDto = _mapper.Map<List<UserReadDto>>(teachers);
            return Ok(teachersDto);
        }

        [HttpPost]
        public async Task<ActionResult<TeacherReadDto>> CreateTeacher([FromBody] UserCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserName))
                return BadRequest("Nome do professor é obrigatório.");

            var teacher = new User
            {
                UserName = dto.UserName,
                UserEmail = dto.UserEmail,
                PasswordHash = dto.Password,
                PhoneNumber = dto.PhoneNumber,
                Profile = UserProfile.Teacher,
                BirthDate = dto.BirthDate,
                CPF = dto.CPF,
                Role = string.IsNullOrEmpty(dto.Role) ? "Teacher" : dto.Role
            };

            _context.Users.Add(teacher);
            await _context.SaveChangesAsync();

            var teacherDto = _mapper.Map<TeacherReadDto>(teacher);
            return CreatedAtAction(nameof(GetTeachers), new { id = teacher.Id }, teacherDto);
        }
    }
}
