namespace EducationalPlataform.Entities
{
    public class AssessmentAnswer
    {
        public int Id { get; set; }
        public int AttemptId { get; set; }
        public AssessmentAttempt Attempt { get; set; } = null!;
        public int QuestionId { get; set; }
        public int SelectedOptionId { get; set; }
    }
}
