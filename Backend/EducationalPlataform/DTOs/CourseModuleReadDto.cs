namespace EducationalPlataform.DTOs
{
    public class CourseModuleReadDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string? Description { get; set; }

        public int Order { get; set; }
        public bool IsPublished { get; set; }
        public int CourseId { get; set; }

        public List<LessonReadDto> Lessons { get; set; } = new();
    }
}