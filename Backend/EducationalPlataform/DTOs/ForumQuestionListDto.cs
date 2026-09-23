namespace EducationalPlataform.DTOs
{
    public class ForumQuestionListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsResolved { get; set; }
        public int ReplyCount { get; set; }
        public int CourseId { get; set; }
        public int? LessonId { get; set; }
        public string? LessonTitle { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}
