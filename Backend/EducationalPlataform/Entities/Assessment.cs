using EducationalPlataform.Models.Enums;

namespace EducationalPlataform.Entities
{
    public class Assessment
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AssessmentType Type { get; set; } = AssessmentType.Activity;
        public decimal PassingScore { get; set; } = 70;
        public bool IsPublished { get; set; } = true;
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<AssessmentQuestion> Questions { get; set; } = new List<AssessmentQuestion>();
        public ICollection<AssessmentAttempt> Attempts { get; set; } = new List<AssessmentAttempt>();
    }
}
