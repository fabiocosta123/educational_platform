using AutoMapper;

namespace EducationalPlataform.DTOs
{
    public class UserReadDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string CPF { get; set; }
        public DateTime BirthDate { get; set; }
        public Profile UserProfile { get; set; }
    }
}
