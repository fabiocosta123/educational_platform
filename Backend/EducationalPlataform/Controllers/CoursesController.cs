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
                    .Include(c => c.Teacher)
                    .Include(c => c.Lessons)
                    .Include(c => c.EnrolledUsers)
                        .ThenInclude(e => e.User)                    
                    .ToListAsync();

                var coursesDto = _mapper.Map<List<CourseReadDto>>(courses);
                return Ok(coursesDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.ToString());
            }


        }



        [HttpGet("{id}")]
        public async Task<ActionResult<CourseReadDto>> GetById(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Teacher)
                .Include(c => c.Lessons)
                .Include(c => c.EnrolledUsers)
                    .ThenInclude(e => e.User)                
                .FirstOrDefaultAsync(c => c.Id == id);


            if (course == null)
                return NotFound();

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
            
            var course = _mapper.Map<Course>(dto);

            
            var teacher = _context.Users
                .FirstOrDefault(u => u.Id == dto.TeacherId && u.Profile == UserProfile.Teacher);

            if (teacher == null)
                return BadRequest("Professor não encontrado.");

            course.Teacher = teacher;
            course.TeacherId = teacher.Id;

            teacher.Courses.Add(course);


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
            var course = _context.Courses
                .Include(c => c.Teacher)
                .FirstOrDefault(c => c.Id == id);

            if (course == null) return NotFound();

            
            var oldTeacher = course.Teacher;

            
            var newTeacher = _context.Users
                .Include(t => t.Courses)
                .FirstOrDefault(u => u.Id == dto.TeacherId && u.Profile == UserProfile.Teacher);

            if (newTeacher == null)
                return BadRequest("Professor não encontrado.");

            // atualiza dados do curso
            course.Title = dto.Title;
            course.Description = dto.Description;
            course.TeacherId = dto.TeacherId;
            course.Teacher = newTeacher;

            // remove o curso da lista do professor antigo
            if (oldTeacher != null && oldTeacher.Id != newTeacher.Id)
            {
                oldTeacher.Courses.Remove(course);
            }

            // adiciona o curso à lista do novo professor
            if (!newTeacher.Courses.Contains(course))
            {
                newTeacher.Courses.Add(course);
            }

            _context.SaveChanges();

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
                .Include(c => c.Teacher)
                .Include(c => c.Lessons)
                .Include(c => c.EnrolledUsers)
                    .ThenInclude(e => e.User)
                .Include(c => c.Teacher)
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
                .Include(c => c.Teacher)
                .Include(c => c.Lessons)
                .Include(c => c.EnrolledUsers)
                    .ThenInclude(e => e.User)
                .Include(c => c.Teacher)
                .Where(c => c.CreatorId == id)
                .ToListAsync();


            var coursesDto = _mapper.Map<List<CourseReadDto>>(courses);
            return Ok(coursesDto);
        }
    }
}
