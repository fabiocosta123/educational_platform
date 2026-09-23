using System.ComponentModel.DataAnnotations;
using EducationalPlataform.Validation;

namespace EducationalPlataform.DTOs
{
    public class PublicCourseRegisterDto
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "A senha deve possuir pelo menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;

        
        [Required(ErrorMessage = "E-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string UserEmail { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "CPF é obrigatório.")]
        [Cpf]
        public string CPF { get; set; } = string.Empty;
        [Required(ErrorMessage = "Telefone é obrigatório.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Data de nascimento é obrigatória.")]
        public DateTime BirthDate { get; set; }
    }
}
