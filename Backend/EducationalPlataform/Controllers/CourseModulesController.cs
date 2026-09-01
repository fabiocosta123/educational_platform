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
    public class CourseModulesController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;
        private readonly IMapper _mapper;

        public CourseModulesController(
            EducationalPlataformContext context,
            IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        private bool TryGetUserId(out int userId)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(claim, out userId);
        }

        // GET: api/CourseModules
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseModuleReadDto>>> GetAll()
        {
            var modules = await _context.CourseModules
                .AsNoTracking()
                .Include(m => m.Lessons)
                    .ThenInclude(l => l.Teacher)
                .OrderBy(m => m.CourseId)
                .ThenBy(m => m.Order)
                .ToListAsync();

            return Ok(_mapper.Map<List<CourseModuleReadDto>>(modules));
        }

        // GET: api/CourseModules/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CourseModuleReadDto>> GetById(int id)
        {
            var module = await _context.CourseModules
                .AsNoTracking()
                .Include(m => m.Lessons)
                    .ThenInclude(l => l.Teacher)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null)
                return NotFound("Módulo não encontrado.");

            return Ok(_mapper.Map<CourseModuleReadDto>(module));
        }

        // GET: api/CourseModules/course/{courseId}
        [HttpGet("course/{courseId}")]
        public async Task<ActionResult<IEnumerable<CourseModuleReadDto>>> GetByCourse(
            int courseId)
        {
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == courseId);

            if (!courseExists)
                return NotFound("Curso não encontrado.");

            var modules = await _context.CourseModules
                .AsNoTracking()
                .Where(m => m.CourseId == courseId)
                .Include(m => m.Lessons)
                    .ThenInclude(l => l.Teacher)
                .OrderBy(m => m.Order)
                .ToListAsync();

            return Ok(_mapper.Map<List<CourseModuleReadDto>>(modules));
        }

        // POST: api/CourseModules
        [HttpPost]
        public async Task<ActionResult<CourseModuleReadDto>> Create(
            [FromBody] CourseModuleCreateDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Usuário autenticado inválido.");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == dto.CourseId);

            if (course == null)
                return NotFound("Curso não encontrado.");

            // Somente o professor responsável pelo curso pode criar módulos.
            if (course.TeacherId != userId)
            {
                return Forbid();
            }

            var module = _mapper.Map<CourseModule>(dto);

            _context.CourseModules.Add(module);

            await _context.SaveChangesAsync();

            var result = await _context.CourseModules
                .AsNoTracking()
                .Include(m => m.Lessons)
                    .ThenInclude(l => l.Teacher)
                .FirstAsync(m => m.Id == module.Id);

            return CreatedAtAction(
                nameof(GetById),
                new { id = module.Id },
                _mapper.Map<CourseModuleReadDto>(result));
        }

        // PUT: api/CourseModules/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<CourseModuleReadDto>> Update(
            int id,
            [FromBody] CourseModuleUpdateDto dto)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Usuário autenticado inválido.");

            var module = await _context.CourseModules
                .Include(m => m.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null)
                return NotFound("Módulo não encontrado.");

            // Somente o professor responsável pelo curso pode editar.
            if (module.Course.TeacherId != userId)
            {
                return Forbid();
            }

            module.Name = dto.Name;
            module.Description = dto.Description;
            module.Order = dto.Order;
            module.IsPublished = dto.IsPublished;

            await _context.SaveChangesAsync();

            var result = await _context.CourseModules
                .AsNoTracking()
                .Include(m => m.Lessons)
                    .ThenInclude(l => l.Teacher)
                .FirstAsync(m => m.Id == id);

            return Ok(_mapper.Map<CourseModuleReadDto>(result));
        }

        // DELETE: api/CourseModules/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized("Usuário autenticado inválido.");

            var module = await _context.CourseModules
                .Include(m => m.Course)
                .Include(m => m.Lessons)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (module == null)
                return NotFound("Módulo não encontrado.");

            // Somente o professor responsável pelo curso pode excluir.
            if (module.Course.TeacherId != userId)
            {
                return Forbid();
            }

            if (module.Lessons.Any())
            {
                return BadRequest(
                    "Não é possível excluir um módulo que possui aulas.");
            }

            _context.CourseModules.Remove(module);

            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}