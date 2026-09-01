using EducationalPlataform.Models.Enums;
using EducationalPlataform.Profiles;

public class StudentUpdateDto
{
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime BirthDate { get; set; }
    public UserProfile Profile { get; set; }

    public int CurrentCourseId { get; set; }

    public int NewCourseId { get; set; }

    public string Status { get; set; } = "Ativo";


}



