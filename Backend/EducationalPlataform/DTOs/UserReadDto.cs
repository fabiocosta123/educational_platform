using AutoMapper;

namespace EducationalPlataform.DTOs
{
    public class UserReadDto
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? CPF { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime BirthDate { get; set; }
        public Profile? UserProfile { get; set; }
        public string Role { get; set; } = string.Empty;

        //curso em que o aluno está matriculado
        public List<CourseEnrollmentReadDto> CourseEnrolled { get; set; } = new();

        // cursos criados (se for professor/coordenador)
        public List<CourseReadDto> CoursesCreated { get; set; } = new();
    }
}
