using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class ForumReplyReadDto
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string UserProfile { get; set; } = string.Empty;
    }
}
