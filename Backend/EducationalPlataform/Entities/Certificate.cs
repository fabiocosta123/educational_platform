namespace EducationalPlataform.Entities
{
    public class Certificate
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public string Code { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public decimal ExamAverage { get; set; }
    }
}
