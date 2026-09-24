using EducationalPlataform.Models.Enums;

namespace EducationalPlataform.DTOs
{
    public class AssessmentOptionWriteDto
    {
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class AssessmentQuestionWriteDto
    {
        public string Prompt { get; set; } = string.Empty;
        public decimal Points { get; set; } = 1;
        public List<AssessmentOptionWriteDto> Options { get; set; } = new();
    }

    public class AssessmentCreateDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public AssessmentType Type { get; set; }
        public decimal PassingScore { get; set; } = 70;
        public bool IsPublished { get; set; } = true;
        public List<AssessmentQuestionWriteDto> Questions { get; set; } = new();
    }

    public class AssessmentAnswerSubmitDto
    {
        public int QuestionId { get; set; }
        public int OptionId { get; set; }
    }

    public class AssessmentSubmitDto
    {
        public List<AssessmentAnswerSubmitDto> Answers { get; set; } = new();
    }
}
