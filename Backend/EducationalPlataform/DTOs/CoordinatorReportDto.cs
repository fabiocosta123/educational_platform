

namespace EducationalPlataform.DTOs
{
    public class CoordinatorReportDto
    {
        public int CoursesCount { get; set; }
        public int TeachersCount { get; set; }
        public int StudentsCount { get; set; }
        public int LessonsCount { get; set; }
        public int AvgProgress { get; set; }


        public EnrollmentReportDto Enrollments { get; set; } = new();

        public List<CourseReportDto> Courses { get; set; } = new();
        public FinanceSummaryDto Financial { get; set; } = new();
    }


    public class EnrollmentReportDto
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Active { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
    
    }

    public class CourseReportDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int StudentsCount { get; set; }
        public int Progress { get; set; }
    }
}
