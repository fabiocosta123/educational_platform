using AutoMapper;
using EducationalPlataform.Data;
using EducationalPlataform.Entities;
using EducationalPlataform.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EducationalPlataform.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LessonsController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;
        private readonly IMapper _mapper;


        public LessonsController(EducationalPlataformContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<LessonReadDto>> GetAll()
        {
            var lessons = _context.Lessons.ToList();
            var lessonsDto = _mapper.Map<List<LessonReadDto>>(lessons);

            return Ok(lessonsDto);
        }

        [HttpGet("{id}")]
        public ActionResult<LessonReadDto> GetById(int id)
        {
            var lesson = _context.Lessons.Find(id);
            if (lesson == null)
                throw new ArgumentException($"Course with id {id} not found");

            var lessonDto = _mapper.Map<LessonReadDto>(lesson);
            return Ok(lessonDto);
        }

        [HttpPost]
        public async Task<ActionResult<LessonReadDto>> Create([FromBody] LessonCreateDto dto)
        {
            var teacherId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var lesson = new Lesson(
                dto.Title,
                dto.Content,
                dto.Date,
                dto.CourseId,
                teacherId
             );

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            var lessonReadDto = _mapper.Map<LessonReadDto>(lesson);

            return CreatedAtAction(nameof(GetById), new { id = lesson.Id }, lessonReadDto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] LessonUpdateDto dto)
        {
            var lesson = _context.Lessons.FindAsync(id);
            if (lesson == null)
                throw new ArgumentException($"Lesson with id {id} not found");

            _mapper.Map(dto, lesson);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            var lesson = _context.Lessons.Find(id);
            if (lesson == null)
                throw new ArgumentException($"Course with id {id} not found");
            _context.Lessons.Remove(lesson);
            _context.SaveChanges();

            return NoContent();
        }
       
    }
}
