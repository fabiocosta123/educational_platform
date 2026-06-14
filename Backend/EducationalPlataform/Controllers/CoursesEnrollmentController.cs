using AutoMapper;
using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace EducationalPlataform.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesEnrollmentController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;
        private readonly IMapper _mapper;

        public CoursesEnrollmentController(EducationalPlataformContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CourseEnrollmentReadDto>> GetAll([FromBody] int? userId)
        {
            var query = _context.CourseEnrollments.AsQueryable();
            
            if (userId.HasValue)
            {
                query = query.Where(e => e.UserId == userId.Value);
            }
            var enrollments = query
                .Include(e => e.User)
                .Include(e => e.Course)
                .ToList();

            var enrollmentsDto = _mapper.Map<List<CourseEnrollmentReadDto>>(enrollments);
            return Ok(enrollmentsDto);
        }

        [HttpGet("{id}")]
        public ActionResult<CourseEnrollmentReadDto> GetById(int id)
        {
            var enrollment = _context.CourseEnrollments.Find(id);
            if (enrollment == null)
                throw new ArgumentException($"Course with id {id} not found");

            var enrollmentDto = _mapper.Map<CourseEnrollmentReadDto>(enrollment);
            return Ok(enrollmentDto);
        }

        [HttpPost]
        public ActionResult<CourseEnrollmentReadDto> Create([FromBody] CourseEnrollmentCreateDto dto)
        {
            var enrollment = _mapper.Map<CourseEnrollment>(dto);

            _context.CourseEnrollments.Add(enrollment);
            _context.SaveChanges();

            var enrollmentReadDto = _mapper.Map<CourseEnrollmentReadDto>(enrollment);
            return CreatedAtAction(nameof(GetById), new { id = enrollment.Id }, enrollmentReadDto);
        }

        [HttpPut("{id}")]
        public ActionResult Update(int id, [FromBody] CourseEnrollmentUpdateDto dto)
        {
            var enrollment = _context.CourseEnrollments.Find(id);
            if (enrollment == null)
                throw new ArgumentException($"Course with id {id} not found");

            _mapper.Map(dto, enrollment);
            _context.SaveChanges();

            return NoContent();
        }


        [HttpDelete("{id}")]
        public ActionResult Delete(int id)
        {
            var enrollment = _context.CourseEnrollments.Find(id);
            if (enrollment == null)
                throw new ArgumentException($"Course with id {id} not found");

            _context.CourseEnrollments.Remove(enrollment);
            _context.SaveChanges();
            return NoContent();
        }
    }
}
