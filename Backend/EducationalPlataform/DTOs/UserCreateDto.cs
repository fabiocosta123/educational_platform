using EducationalPlataform.Models.Enums;
using System.ComponentModel.DataAnnotations;

public class UserCreateDto
{
    [Required(ErrorMessage = "Usuário é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome de usuário deve ter no máximo 100 caracteres.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email é obrigatório.")]
    [EmailAddress(ErrorMessage = "O email deve ser válido.")]
    public string UserEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string Password { get; set; } = string.Empty;

    
    [RegularExpression(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$", ErrorMessage = "Formato inválido de CPF.")]
    public string CPF { get; set; } = string.Empty;

    [Range(typeof(DateTime), "1900-01-01", "2100-12-31", ErrorMessage = "Data de nascimento deve estar entre 1900 e 2100.")]
    public DateTime BirthDate { get; set; }

    [Required(ErrorMessage = "Perfil é obrigatório.")]
    public UserProfile Profile { get; set; }
    public string Role { get; set; } = string.Empty.ToLower();
}
