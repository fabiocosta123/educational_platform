namespace EducationalPlataform.Entities
{
    public class ForumReply
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Pergunta
        public int ForumQuestionId { get; set; }
        public ForumQuestion ForumQuestion { get; set; } = null!;

        // Usuário que respondeu
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        // Identifica se a resposta veio de professor/coordenador
        // sem precisar duplicar informação.
    }
}