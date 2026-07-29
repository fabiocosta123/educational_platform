using EducationalPlataform.Models.Enums;
using EducationalPlataform.Profiles;

public class StudentUpdateDto
{
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime BirthDate { get; set; }
    public UserProfile Profile { get; set; }

    // Novo campo para manter cursos
    public List<CourseEnrollmentDto> CourseEnrollments { get; set; } = new();
}

public class CourseEnrollmentDto
{
    public int CourseId { get; set; }
    public string Status { get; set; } = string.Empty;
}

