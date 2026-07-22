using EducationalPlataform.Models.Enums;
using System.ComponentModel.DataAnnotations;


namespace EducationalPlataform.Entities
{
    public class User
    {
        public int Id { get; set; }
       
        public string? UserName { get; set; }

        [EmailAddress(ErrorMessage = "Email inválido.")]
        public string? UserEmail { get; set; }

        
        public string? PasswordHash { get; set; }
       
        public string? CPF { get; set; }

       
        public DateTime BirthDate { get; set; }

        
        public UserProfile Profile { get; set; }

        public string Role { get; set; }

        public string? PhoneNumber { get; set; }

        // relacionships
        public ICollection<Course> CoursesCreated { get; set; } = new List<Course>();
        public ICollection<CourseEnrollment> CoursesEnrolled { get; set; } = new List<CourseEnrollment>();

        public ICollection<Course> Courses { get; set; } = new List<Course>();

        public ICollection<Lesson> LessonsTaught { get; set; } = new List<Lesson>();

        public ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();


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
            
            if (Enum.TryParse<UserProfile>(profile, true, out var parsedProfile))
            {
                Profile = parsedProfile;
            }
            else
            {
                throw new ArgumentException("Perfil inválido", nameof(profile));
            }
        }
    }
}
