namespace EducationalPlataform.Entities
{
    public class Announcement
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsPublished { get; set; } = true;

        // Quem publicou
        public int AuthorId { get; set; }
        public User Author { get; set; } = null!;

        // Aviso geral ou relacionado a um curso
        public int? CourseId { get; set; }
        public Course? Course { get; set; }

        // Futuramente podemos evoluir para:
        // - aviso para todos
        // - aviso para alunos de um curso
        // - aviso para uma turma
    }
}