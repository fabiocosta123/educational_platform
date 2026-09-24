namespace EducationalPlataform.Entities
{
    public class AssessmentAttempt
    {
        public int Id { get; set; }
        public int AssessmentId { get; set; }
        public Assessment Assessment { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public decimal Score { get; set; }
        public string Status { get; set; } = "Submitted";
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public ICollection<AssessmentAnswer> Answers { get; set; } = new List<AssessmentAnswer>();
    }
}
