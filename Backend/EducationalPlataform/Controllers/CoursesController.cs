using AutoMapper;
using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using Microsoft.AspNetCore.Mvc;
using EducationalPlataform.Middleware;
using Microsoft.AspNetCore.Authorization;

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

        [HttpPost]
        public ActionResult<CourseReadDto> Create([FromBody] CourseCreateDto dto)
        {
            var course = _mapper.Map<Course>(dto);

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
    }
}
