using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
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
            var dto = new CoordinatorDashboardDto
           {
               CoursesCount = await _context.Courses.CountAsync(),
               TeachersCount = await _context.Users.CountAsync(u => u.Profile == UserProfile.Teacher),
               StudentsCount = await _context.Users.CountAsync(u => u.Profile == UserProfile.Student),
               CoordinatorCount = await _context.Users.CountAsync(u => u.Profile == UserProfile.Coordinator),
               NextLessonsCount = await _context.Lessons.CountAsync(l => l.Date >= DateTime.Now),
               AvgProgress = await _context.CourseEnrollments.AnyAsync()
               ? (int)await _context.CourseEnrollments.AverageAsync(e => e.ProgressPercentage) : 0,

               Courses = await _context.Courses
               .Select(c => new CourseSummaryDto
               {
                   Id = c.Id,
                   Title = c.Title,
                   StudentsCount = _context.CourseEnrollments.Count(e => e.CourseId == c.Id),
                   Progress = _context.CourseEnrollments
                   .Where(e => e.CourseId == c.Id)
                   .Any() ? (int)_context.CourseEnrollments
                   .Where(e => e.CourseId == c.Id)
                   .Average(e => e.ProgressPercentage) : 0
               }).ToListAsync(),
           };

            return Ok(dto);
        }
    }
}
