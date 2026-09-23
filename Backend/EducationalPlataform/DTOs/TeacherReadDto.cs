using EducationalPlataform.Entities;

namespace EducationalPlataform.DTOs
{
    public class TeacherReadDto
    {
        public int Id { get; set;  }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CPF { get; set; }
        public DateTime BirthDate { get; set; }
        public ICollection<CourseReadDto> Courses { get; set; } = new List<CourseReadDto>();

    }
}
