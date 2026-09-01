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
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        public async Task<ActionResult<LessonReadDto>> Create(
    [FromForm] LessonCreateDto dto,
    IFormFile? material)
        {
            var teacherIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(teacherIdClaim, out var teacherId))
                return Unauthorized("Professor não identificado.");

            // Verifica se o módulo existe
            var module = await _context.CourseModules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == dto.CourseModuleId);

            if (module == null)
                return BadRequest("Módulo não encontrado.");

            // Verifica se o usuário autenticado é o professor responsável pelo curso
            if (module.Course.TeacherId != teacherId)
                return Forbid();

            string? materialUrl = null;

            // Upload do material
            if (material != null)
            {
                var extension = Path.GetExtension(material.FileName)
                    .ToLowerInvariant();

                var allowedExtensions = new[]
                {
            ".pdf",
            ".txt"
        };

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(
                        "Formato de arquivo não permitido. Envie apenas PDF ou TXT.");
                }

                if (material.Length == 0)
                    return BadRequest("O arquivo enviado está vazio.");

                const long maxFileSize = 10 * 1024 * 1024;

                if (material.Length > maxFileSize)
                {
                    return BadRequest(
                        "O arquivo não pode ultrapassar 10 MB.");
                }

                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "lessons");

                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName =
                    $"{Guid.NewGuid():N}{extension}";

                var filePath = Path.Combine(
                    uploadsFolder,
                    uniqueFileName);

                await using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await material.CopyToAsync(stream);
                }

                materialUrl =
                    $"/uploads/lessons/{uniqueFileName}";
            }

            var lesson = new Lesson
            {
                Title = dto.Title,
                Description = dto.Description,
                VideoUrl = dto.VideoUrl,
                PdfUrl = materialUrl,
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