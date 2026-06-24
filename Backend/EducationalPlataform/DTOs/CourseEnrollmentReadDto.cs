namespace EducationalPlataform.DTOs
{
    public class CourseEnrollmentReadDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }

        
        public string? UserName { get; set; }
        public string? CourseTitle { get; set; }

        public int ProgressPercentage { get; set; }
        public string Status { get; set; }
        public int CompletedLessons { get; set; }
        public int TotalLessons { get; set; }
    }
}
