
namespace EducationalPlataform.DTOs
{
    public class ForumQuestionReadDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsResolved { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public int CourseId { get; set; }

        public string CourseTitle { get; set; } = string.Empty;

        public int? LessonId { get; set; }

        public string? LessonTitle { get; set; }

        public List<ForumReplyReadDto> Replies { get; set; } = new();
    }
}
