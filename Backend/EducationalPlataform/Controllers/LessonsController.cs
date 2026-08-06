using AutoMapper;
using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EducationalPlataform.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LessonsController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;
        private readonly IMapper _mapper;

        public LessonsController(
            EducationalPlataformContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        
        // Lista todas 
       

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LessonReadDto>>> GetAll()
        {
            var lessons = await _context.Lessons
                .AsNoTracking()
                .Include(l => l.Teacher)
                .Include(l => l.CourseModule)
                .OrderBy(l => l.Order)
                .ToListAsync();

            return Ok(_mapper.Map<List<LessonReadDto>>(lessons));
        }

        
        // Buscar por id
        

        [HttpGet("{id}")]
        public async Task<ActionResult<LessonReadDto>> GetById(int id)
        {
            var lesson = await _context.Lessons
                .AsNoTracking()
                .Include(l => l.Teacher)
                .Include(l => l.CourseModule)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
                return NotFound();

            return Ok(_mapper.Map<LessonReadDto>(lesson));
        }

        
        // Estatisticas do professor
        

        [HttpGet("teacher/{teacherId}/stats")]
        public async Task<ActionResult> GetLessonStatsByTeacher(int teacherId)
        {
            var lessons = await _context.Lessons
                .Where(l => l.TeacherId == teacherId)
                .ToListAsync();

            return Ok(new
            {
                totalLessons = lessons.Count,
                publishedLessons = lessons.Count(l => l.IsPublished),
                unpublishedLessons = lessons.Count(l => !l.IsPublished)
            });
        }

        
        // Cria 
        

        [HttpPost]
        public async Task<ActionResult<LessonReadDto>> Create(
            [FromBody] LessonCreateDto dto)
        {
            var teacherId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var lesson = new Lesson
            {
                Title = dto.Title,
                Description = dto.Description,
                VideoUrl = dto.VideoUrl,
                PdfUrl = dto.PdfUrl,
                DurationSeconds = dto.DurationSeconds,
                Order = dto.Order,
                IsPublished = dto.IsPublished,
                CourseModuleId = dto.CourseModuleId,
                TeacherId = teacherId
            };

            _context.Lessons.Add(lesson);

            await _context.SaveChangesAsync();

            await _context.Entry(lesson)
                .Reference(l => l.Teacher)
                .LoadAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = lesson.Id },
                _mapper.Map<LessonReadDto>(lesson));
        }

        
        // Altera 
        

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] LessonUpdateDto dto)
        {
            var lesson = await _context.Lessons
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
                return NotFound();

            lesson.Title = dto.Title;
            lesson.Description = dto.Description;
            lesson.VideoUrl = dto.VideoUrl;
            lesson.PdfUrl = dto.PdfUrl;
            lesson.DurationSeconds = dto.DurationSeconds;
            lesson.Order = dto.Order;
            lesson.IsPublished = dto.IsPublished;
            lesson.CourseModuleId = dto.CourseModuleId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        
        // Excluir 
        

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);

            if (lesson == null)
                return NotFound();

            _context.Lessons.Remove(lesson);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}