namespace EducationalPlataform.Entities
{
    public class AssessmentOption
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public AssessmentQuestion Question { get; set; } = null!;
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int Order { get; set; }
    }
}
