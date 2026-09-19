namespace EducationalPlataform.Entities
{
    public class ForumQuestion
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsResolved { get; set; }

        // Quem fez a pergunta
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Curso relacionado
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        // Opcional: pergunta vinculada a uma aula específica
        public int? LessonId { get; set; }
        public Lesson? Lesson { get; set; }

        // Respostas
        public ICollection<ForumReply> Replies { get; set; } =
            new List<ForumReply>();
    }
}