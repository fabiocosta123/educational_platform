namespace EducationalPlataform.DTOs
{
    public class LessonCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Date {  get; set; }
        public int CourseId { get; set; }
    }
}
