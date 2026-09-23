using System.ComponentModel.DataAnnotations;
using EducationalPlataform.Validation;

namespace EducationalPlataform.DTOs
{
    public class TeacherUpdateDto
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string UserEmail { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public DateTime BirthDate { get; set; }

        [Cpf(AllowEmpty = true)]
        public string? CPF { get; set; }

        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string? Password { get; set; }
    }
}
