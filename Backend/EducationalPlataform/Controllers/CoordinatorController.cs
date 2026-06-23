using EducationalPlataform.Data;
using EducationalPlataform.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoordinatorController : Controller
    {
        private readonly EducationalPlataformContext _context;

        public CoordinatorController(EducationalPlataformContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var coursesCount = await _context.Courses.CountAsync();
            var teachersCount = await _context.Users.CountAsync(u => u.Profile == UserProfile.Teacher);
            var studentsCount = await _context.Users.CountAsync(u => u.Profile == UserProfile.Student);
            var coordinatorCount = await _context.Users.CountAsync(u => u.Profile == UserProfile.Cordinator);
            var nextLessonsCount = await _context.Lessons.CountAsync(l => l.Date >= DateTime.Now);

            var avgProgress = await _context.CourseEnrollments.AnyAsync()
                ? (int)await _context.CourseEnrollments.AverageAsync(e => e.ProgressPercentage) : 0;

            var courses = await _context.Courses
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    studentsCount = _context.CourseEnrollments.Count(e => e.CourseId == c.Id)
                }).ToListAsync();

            return Ok(new
            {
                coursesCount,
                teachersCount,
                studentsCount,
                coordinatorCount,
                nextLessonsCount,
                avgProgress,
                courses
            });
        }
    }
}
