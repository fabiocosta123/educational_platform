using AutoMapper;
using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using EducationalPlataform.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly IPasswordHasher<User> _passwordHasher;

        public TeacherController(
            EducationalPlataformContext context,
            IMapper mapper,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _mapper = mapper;
            _passwordHasher = passwordHasher;
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


        [HttpGet("{teacherId}")]
        public async Task<ActionResult<TeacherReadDto>> GetTeacherById(int teacherId)
        {
            var teacher = await _context.Users
                .Where(u => u.Id == teacherId && u.Profile == UserProfile.Teacher)
                .Include(u => u.CoursesTaught)
                    .ThenInclude(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .Include(u => u.CoursesTaught)
                    .ThenInclude(c => c.EnrolledUsers)
                        .ThenInclude(e => e.User)
                .FirstOrDefaultAsync();

            if (teacher == null)
            {
                return NotFound("Professor não encontrado.");
            }

            var teacherDto = _mapper.Map<TeacherReadDto>(teacher);
            return Ok(teacherDto);
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
                return BadRequest(new { message = "Nome do professor é obrigatório." });

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return BadRequest(new { message = "Senha é obrigatória e deve ter no mínimo 6 caracteres." });

            if (!CpfValidator.IsValid(dto.CPF))
                return BadRequest(new { message = "CPF inválido." });

            var teacher = new User
            {
                UserName = dto.UserName.Trim(),
                UserEmail = dto.UserEmail.Trim(),
                PhoneNumber = dto.PhoneNumber,
                Profile = UserProfile.Teacher,
                BirthDate = dto.BirthDate,
                CPF = CpfValidator.Format(dto.CPF),
                Role = "Teacher"
            };

            teacher.PasswordHash = _passwordHasher.HashPassword(teacher, dto.Password);

            _context.Users.Add(teacher);
            await _context.SaveChangesAsync();

            var teacherDto = _mapper.Map<TeacherReadDto>(teacher);
            return CreatedAtAction(
                nameof(GetTeacherById),
                new { teacherId = teacher.Id },
                teacherDto);
        }

        [Authorize(Roles = "Coordinator")]
        [HttpPut("{teacherId:int}")]
        public async Task<ActionResult<TeacherReadDto>> UpdateTeacher(
            int teacherId,
            [FromBody] TeacherUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var teacher = await _context.Users
                .Include(u => u.CoursesTaught)
                .FirstOrDefaultAsync(u =>
                    u.Id == teacherId &&
                    u.Profile == UserProfile.Teacher);

            if (teacher == null)
                return NotFound(new { message = "Professor não encontrado." });

            if (string.IsNullOrWhiteSpace(dto.UserName))
                return BadRequest(new { message = "Nome do professor é obrigatório." });

            if (!string.IsNullOrWhiteSpace(dto.CPF) && !CpfValidator.IsValid(dto.CPF))
                return BadRequest(new { message = "CPF inválido." });

            if (!string.IsNullOrWhiteSpace(dto.Password) && dto.Password.Length < 6)
                return BadRequest(new { message = "A senha deve ter no mínimo 6 caracteres." });

            teacher.UserName = dto.UserName.Trim();
            teacher.UserEmail = dto.UserEmail.Trim();
            teacher.PhoneNumber = dto.PhoneNumber;
            teacher.BirthDate = dto.BirthDate;

            if (!string.IsNullOrWhiteSpace(dto.CPF))
            {
                teacher.CPF = CpfValidator.Format(dto.CPF);
            }

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                teacher.PasswordHash = _passwordHasher.HashPassword(teacher, dto.Password);
            }

            await _context.SaveChangesAsync();

            var teacherDto = _mapper.Map<TeacherReadDto>(teacher);
            return Ok(teacherDto);
        }
    }
}
