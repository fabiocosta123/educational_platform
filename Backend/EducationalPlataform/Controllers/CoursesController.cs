using AutoMapper;
using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using EducationalPlataform.Models.Enums;

namespace EducationalPlataform.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;
        private readonly IMapper _mapper;

        // constructor to inject the database context
        public CoursesController(EducationalPlataformContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult<IEnumerable<CourseReadDto>> GetAll()
        {
            var courses = _context.Courses.ToList();
            var coursesDto = _mapper.Map<List<CourseReadDto>>(courses);
            return Ok(coursesDto);

        }



        [HttpGet("{id}")]
        public ActionResult<CourseReadDto> GetById(int id)
        {
            var course = _context.Courses.Find(id);
            if (course == null)
                throw new ArgumentException($"Course with id {id} not found");

            var courseDto = _mapper.Map<CourseReadDto>(course);
            return Ok(courseDto);
        }

        [HttpGet("teacher/{teacherId}/count")]
        public ActionResult<int> GetCoursesCountByTeacher(int teacherId)
        {
            var count = _context.Courses.Count(c => c.CreatorId == teacherId);
            return Ok(count);
        }

        [HttpPost]
        public ActionResult<CourseReadDto> Create([FromBody] CourseCreateDto dto)
        {
            // Mapeia os campos básicos
            var course = _mapper.Map<Course>(dto);

            // Carregar professor
            var teacher = _context.Users
                .FirstOrDefault(u => u.Id == dto.TeacherId && u.Profile == UserProfile.Teacher);

            if (teacher == null)
                return BadRequest("Professor não encontrado.");

            course.Teacher = teacher;

            // Criador = coordenador logado
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userId, out var creatorId))
                course.CreatorId = creatorId;

            _context.Courses.Add(course);
            _context.SaveChanges();

            var courseReadDto = _mapper.Map<CourseReadDto>(course);
            return CreatedAtAction(nameof(GetById), new { id = course.Id }, courseReadDto);
        }



        [HttpPut("{id}")]
        public ActionResult Update(int id, [FromBody] CourseUpdateDto dto)
        {
            var course = _context.Courses.Find(id);
            if (course == null)
                throw new ArgumentException($"Course with id {id} not found");

            course.Title = dto.Title;
            course.Description = dto.Description;

            _context.SaveChanges();
            return NoContent();
        }



        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            var course = _context.Courses.Find(id);
            if (course == null)
                throw new ArgumentException($"Course with id {id} not found");
            _context.Courses.Remove(course);
            _context.SaveChanges();
            return NoContent();
        }

        [Authorize(Roles = "Teacher")]
        [HttpGet("teacher/{teacherId}")]
        public async Task<ActionResult<IEnumerable<CourseReadDto>>> GetCoursesByTeacher(int teacherId)
        {
            var courses = await _context.Courses
                .Where(c => c.CreatorId == teacherId)
                .ToListAsync();

            var coursesDto = _mapper.Map<List<CourseReadDto>>(courses);
            return Ok(coursesDto);
        }

        [Authorize(Roles = "Teacher")]
        [HttpGet("my-courses")]
        public async Task<ActionResult<IEnumerable<CourseReadDto>>> GetMyCourses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (!int.TryParse(userId, out var id)) return Unauthorized();

            var courses = await _context.Courses
                .Where(c => c.CreatorId == id)
                .ToListAsync();

            var coursesDto = _mapper.Map<List<CourseReadDto>>(courses);
            return Ok(coursesDto);
        }
    }
}
