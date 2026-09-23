using System.ComponentModel.DataAnnotations;
using EducationalPlataform.Models.Enums;

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

    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
    public string? Password { get; set; }
}



