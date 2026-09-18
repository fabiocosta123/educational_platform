using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EducationalPlataform.Entities;


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
                NextLessonsCount = await _context.Lessons.CountAsync(),
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


        [HttpGet("reports")]
        public async Task<IActionResult> GetReports()
        {
            var enrollments = await _context.CourseEnrollments
                .AsNoTracking()
                .ToListAsync();

            var financialQuery = _context.Payments.AsNoTracking();

            var totalPayments = await financialQuery.CountAsync();

            var totalReceived = await financialQuery
                .Where(p => p.Status == PaymentStatus.Paid)
                .SumAsync(p => p.Amount);

            var totalPending = await financialQuery
                .Where(p => p.Status == PaymentStatus.Pending)
                .SumAsync(p => p.Amount);

            var now = DateTime.Now;

            var monthlyRevenue = await financialQuery
                .Where(p =>
                    p.Status == PaymentStatus.Paid &&
                    p.PaidAt.HasValue &&
                    p.PaidAt.Value.Month == now.Month &&
                    p.PaidAt.Value.Year == now.Year)
                .SumAsync(p => p.Amount);

            var paidPayments = await financialQuery
                .CountAsync(p => p.Status == PaymentStatus.Paid);

            var pendingPayments = totalPayments - paidPayments;

            var activeStudents = await _context.CourseEnrollments
                .Where(e =>
                    e.Status == "Active" ||
                    e.Status == "Ativo")
                .Select(e => e.UserId)
                .Distinct()
                .CountAsync();

            var financial = new FinanceSummaryDto
            {
                TotalReceived = totalReceived,
                TotalPending = totalPending,
                TotalPayments = totalPayments,
                Paid = paidPayments,
                Pending = pendingPayments,
                DefaultRate = totalPayments == 0
                    ? 0
                    : (decimal)pendingPayments / totalPayments * 100,
                ActiveStudents = activeStudents,
                MonthlyRevenue = monthlyRevenue,
                Message = totalPayments == 0
                    ? "Nenhum dado financeiro encontrado."
                    : null
            };

            var reports = new CoordinatorReportDto
            {
                CoursesCount = await _context.Courses.CountAsync(),

                TeachersCount = await _context.Users
                    .CountAsync(u => u.Profile == UserProfile.Teacher),

                StudentsCount = await _context.Users
                    .CountAsync(u => u.Profile == UserProfile.Student),

                LessonsCount = await _context.Lessons.CountAsync(),

                AvgProgress = enrollments.Any()
                    ? (int)Math.Round(
                        enrollments.Average(e => e.ProgressPercentage))
                    : 0,


                Financial = financial,

                Enrollments = new EnrollmentReportDto
                {
                    Total = enrollments.Count,

                    Pending = enrollments.Count(e =>
                        e.Status == "Pending" ||
                        e.Status == "Pendente"),

                    Active = enrollments.Count(e =>
                        e.Status == "Active" ||
                        e.Status == "Ativo"),

                    Completed = enrollments.Count(e =>
                        e.Status == "Completed" ||
                        e.Status == "Concluido" ||
                        e.Status == "Concluído"),

                    Cancelled = enrollments.Count(e =>
                        e.Status == "Cancelled" ||
                        e.Status == "Cancelado")
                },

                Courses = await _context.Courses
                    .Select(c => new CourseReportDto
                    {
                        Id = c.Id,
                        Title = c.Title,

                        StudentsCount = _context.CourseEnrollments
                            .Count(e => e.CourseId == c.Id),

                        Progress = _context.CourseEnrollments
                            .Where(e => e.CourseId == c.Id)
                            .Any()
                                ? (int)Math.Round(
                                    _context.CourseEnrollments
                                        .Where(e => e.CourseId == c.Id)
                                        .Average(e => e.ProgressPercentage))
                                : 0
                    })
                    .ToListAsync()
            };

            return Ok(reports);
        }
    }
}
