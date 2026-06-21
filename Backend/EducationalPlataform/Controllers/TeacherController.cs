using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EducationalPlataform.Data;

namespace EducationalPlataform.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;

        public TeacherController(EducationalPlataformContext context)
        {
            _context = context;
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
    }
}
