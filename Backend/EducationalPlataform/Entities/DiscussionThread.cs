namespace EducationalPlataform.Entities
{
    public class DiscussionThread
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public int? LessonId { get; set; }
        public Lesson? Lesson { get; set; }
        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsClosed { get; set; } = false;
        public ICollection<DiscussionPost> Posts { get; set; } = new List<DiscussionPost>();
    }
}
