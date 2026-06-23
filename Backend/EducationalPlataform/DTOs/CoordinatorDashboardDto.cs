namespace EducationalPlataform.DTOs
{

    public class CoordinatorDashboardDto
    {
        public int CoursesCount { get; set; }
        public int TeachersCount { get; set; }
        public int StudentsCount { get; set; }
        public int CoordinatorCount { get; set; }
        public int NextLessonsCount { get; set; }
        public int AvgProgress {  get; set; }
        public List<CourseSummaryDto> Courses { get; set; }
    }


    public class CourseSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int StudentsCount { get; set; }
    }
}
