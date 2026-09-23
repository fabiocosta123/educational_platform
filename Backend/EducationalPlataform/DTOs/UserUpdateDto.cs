using AutoMapper;
using EducationalPlataform.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class UserUpdateDto
    {
        [Required(ErrorMessage = "O campo UserName é obrigatório.")]
        [StringLength(100, ErrorMessage = "O campo UserName deve ter no máximo 100 caracteres.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "O campo UserEmail é obrigatório.")]
        [EmailAddress(ErrorMessage = "O campo UserEmail deve ser um endereço de email válido.")]
        public string UserEmail { get; set; } = string.Empty;

        public string PhoneNumber { get; set; }

        [Range(typeof(DateTime), "01-01-1900", "31-12-2100", ErrorMessage = "O campo BirthDate deve ser uma data válida entre 01/01/1900 e 31/12/2100.")]
        public DateTime BirthDate { get; set;  }

        [Required(ErrorMessage = "O campo Perfil é obrigatório.")]
        public UserProfile Profile { get; set; }

    }
}
