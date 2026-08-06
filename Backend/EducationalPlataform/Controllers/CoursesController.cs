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

       
        public CoursesController(EducationalPlataformContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

       
        

        
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseReadDto>>> GetAll()
        {
            try
            {
                var courses = await _context.Courses
                    .AsNoTracking()
                    .Include(c => c.Teacher)
                    .Include(c => c.Modules)
                        .ThenInclude(m => m.Lessons)
                            .ThenInclude(l => l.Teacher)
                    .Include(c => c.EnrolledUsers)
                        .ThenInclude(e => e.User)
                    .ToListAsync();

                return Ok(_mapper.Map<List<CourseReadDto>>(courses));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }



        [HttpGet("{id}")]
        public async Task<ActionResult<CourseReadDto>> GetById(int id)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Teacher)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                        .ThenInclude(l => l.Teacher)
                .Include(c => c.EnrolledUsers)
                    .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound();

            return Ok(_mapper.Map<CourseReadDto>(course));
        }

        [HttpGet("teacher/{teacherId}/count")]
        public ActionResult<int> GetCoursesCountByTeacher(int teacherId)
        {
            var count = _context.Courses.Count(c => c.CreatorId == teacherId);
            return Ok(count);
        }

        [HttpPost]
        public async Task<ActionResult<CourseReadDto>> Create(CourseCreateDto dto)
        {
            var teacher = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == dto.TeacherId &&
                    u.Profile == UserProfile.Teacher);

            if (teacher == null)
                return BadRequest("Professor não encontrado.");

            var course = _mapper.Map<Course>(dto);

            course.TeacherId = teacher.Id;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userId, out var creatorId))
                course.CreatorId = creatorId;

            _context.Courses.Add(course);

            await _context.SaveChangesAsync();

            await _context.Entry(course)
                .Reference(c => c.Teacher)
                .LoadAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = course.Id },
                _mapper.Map<CourseReadDto>(course));
        }




        [HttpPut("{id}")]
        public async Task<ActionResult<CourseReadDto>> Update(int id, CourseUpdateDto dto)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound();

            var teacher = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Id == dto.TeacherId &&
                    u.Profile == UserProfile.Teacher);

            if (teacher == null)
                return BadRequest("Professor não encontrado.");

            course.Title = dto.Title;
            course.Description = dto.Description;
            course.TeacherId = dto.TeacherId;

            await _context.SaveChangesAsync();

            await _context.Entry(course)
                .Reference(c => c.Teacher)
                .LoadAsync();

            return Ok(_mapper.Map<CourseReadDto>(course));
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
                .AsNoTracking()
                .Where(c => c.CreatorId == teacherId)
                .Include(c => c.Teacher)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                        .ThenInclude(l => l.Teacher)
                .Include(c => c.EnrolledUsers)
                    .ThenInclude(e => e.User)
                .ToListAsync();

            return Ok(_mapper.Map<List<CourseReadDto>>(courses));
        }

        [Authorize(Roles = "Teacher")]
        [HttpGet("my-courses")]
        public async Task<ActionResult<IEnumerable<CourseReadDto>>> GetMyCourses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out var id))
                return Unauthorized();

            var courses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.CreatorId == id)
                .Include(c => c.Teacher)
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                        .ThenInclude(l => l.Teacher)
                .Include(c => c.EnrolledUsers)
                    .ThenInclude(e => e.User)
                .ToListAsync();

            return Ok(_mapper.Map<List<CourseReadDto>>(courses));
        }
    }
}
