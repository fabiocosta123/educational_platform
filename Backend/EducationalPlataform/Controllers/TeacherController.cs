using AutoMapper;
using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Controllers
{
    [Authorize(Roles = "Coordinator,Teacher")]
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;
        private readonly IMapper _mapper;

        public TeacherController(EducationalPlataformContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet("{teacherId}/dashboard")]
        public ActionResult<object> GetDashboard(int teacherId)
        {
            
            var courses = _context.Courses
                .Where(c => c.CreatorId == teacherId)
                .Include(c => c.Lessons)              
                .Include(c => c.EnrolledUsers)    
                .ToList();

            var coursesCount = courses.Count;
            var lessonsCount = courses.Sum(c => c.Lessons.Count);
            var studentsCount = courses.Sum(c => c.EnrolledUsers.Count);

            var nextLesson = courses
                .SelectMany(c => c.Lessons)
                .Where(l => l.Date > DateTime.Now)
                .OrderBy(l => l.Date)
                .FirstOrDefault();

            return Ok(new
            {
                coursesCount,
                lessonsCount,
                studentsCount,
                nextLessonDate = nextLesson?.Date
            });
        }


        [HttpGet]
        public ActionResult<IEnumerable<TeacherReadDto>> GetTeachers()
        {
            var teachers = _context.Users
                .Where(u => u.Profile == UserProfile.Teacher)
                .ToList();

            var teachersDto = _mapper.Map<List<TeacherReadDto>>(teachers);
            return Ok(teachersDto);
        }

    }
}
