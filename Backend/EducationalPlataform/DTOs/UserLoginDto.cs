using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class UserLoginDto
    {
        [Required(ErrorMessage ="Nome é obrigatório")]
        public string UserName { get; set; } = string.Empty;
        [Required(ErrorMessage ="Senha é obrigatório")]
        public string Password { get; set; } = string.Empty;
    }

}
