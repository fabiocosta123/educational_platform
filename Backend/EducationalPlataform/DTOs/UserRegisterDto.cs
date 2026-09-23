using EducationalPlataform.Models.Enums;
using EducationalPlataform.Validation;
using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class UserRegisterDto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string UserEmail { get; set; }
        [Cpf(AllowEmpty = true)]
        public string CPF {  get; set; }
        public string PhoneNumber { get; set; }
        public DateTime BirthDate { get; set; }
        public int Profile { get; set; }
    }
}
