using EducationalPlataform.Models.Enums;
using Microsoft.OpenApi.MicrosoftExtensions;
using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.Entities
{
    public class User
    {
        public int Id { get; set; }
       
        public string? UserName { get; set; }

        public string? UserEmail { get; set; }

        
        public string? PasswordHash { get; set; }
       
        public string CPF { get; set; }

       
        public DateTime BirthDate { get; set; }

        
        public UserProfile Profile { get; set; }


        // relacionships
        public ICollection<Course> CoursesCreated { get; set; } = new List<Course>();
        public ICollection<CourseEnrollment> CoursesEnrolled { get; set; } = new List<CourseEnrollment>();

        // constructor
        public User() { }

        public User(int id, string? userName, string? userEmail, string? passwordHash, string cpf, DateTime birthDate, string profile)
        {
            Id = id;
            UserName = userName;
            UserEmail = userEmail;
            PasswordHash = passwordHash;
            CPF = cpf;
            BirthDate = birthDate;
            Profile = Enum.Parse<UserProfile>(profile);
        }
    }
}
