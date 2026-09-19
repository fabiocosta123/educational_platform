namespace EducationalPlataform.DTOs
{
    public class AnnouncementReadDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsPublished { get; set; }

        public int AuthorId { get; set; }

        public string AuthorName { get; set; } = string.Empty;

        public int? CourseId { get; set; }

        public string? CourseTitle { get; set; }
    }
}