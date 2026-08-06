namespace EducationalPlataform.DTOs
{
    public class LessonReadDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string VideoUrl { get; set; } = string.Empty;

        public string? PdfUrl { get; set; }

        public int Order { get; set; }

        public int DurationSeconds { get; set; }

        public bool IsPublished { get; set; }

        public int CourseModuleId { get; set; }

        public string? TeacherName { get; set; }
    }
}