namespace EducationalPlataform.DTOs
{
    public class CourseReadDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? TeacherName { get; set; }
        public int LessonsCount { get; set; }

        public List<LessonReadDto> Lessons { get; set; } = new();
        public List<CourseEnrollmentReadDto> EnrolledUsers { get; set; } = new();
    }
}
