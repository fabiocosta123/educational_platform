using EducationalPlataform.Models.Enums;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EducationalPlataform.DTOs
{
    public class UserRegisterDto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string UserEmail { get; set; }
        public string CPF {  get; set; }
        public DateTime BirthDate { get; set; }
        public UserProfile Profile { get; set; }
    }
}
