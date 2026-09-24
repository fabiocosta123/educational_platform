namespace EducationalPlataform.Entities
{
    public class AssessmentQuestion
    {
        public int Id { get; set; }
        public int AssessmentId { get; set; }
        public Assessment Assessment { get; set; } = null!;
        public string Prompt { get; set; } = string.Empty;
        public int Order { get; set; }
        public decimal Points { get; set; } = 1;

        public ICollection<AssessmentOption> Options { get; set; } = new List<AssessmentOption>();
    }
}
