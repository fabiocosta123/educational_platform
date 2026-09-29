using System.ComponentModel.DataAnnotations;
using EducationalPlataform.Validation;

namespace EducationalPlataform.DTOs
{
    public class StudentCreateDto
    {
        public string UserName { get; set; }

        [Required(ErrorMessage = "E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string UserEmail { get; set; }
        [Required(ErrorMessage = "CPF é obrigatório.")]
        [Cpf]
        public string CPF { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime BirthDate { get; set; }

        public int CourseId { get; set; }
        
        public string Status { get; set; } = "Ativo";

        [Required(ErrorMessage = "Senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
        public string Password { get; set; } = string.Empty;
    }
}
