namespace EducationalPlataform.DTOs
{
    public class CourseEnrollmentReadDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }

        // Opcional: incluir informações adicionais
        public string? UserName { get; set; }
        public string? CourseTitle { get; set; }
    }
}
