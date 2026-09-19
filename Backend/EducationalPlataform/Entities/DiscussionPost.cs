namespace EducationalPlataform.Entities
{
    public class DiscussionPost
    {
        public int Id { get; set; }

        public int DiscussionThreadId { get; set; }
        public DiscussionThread DiscussionThread { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}