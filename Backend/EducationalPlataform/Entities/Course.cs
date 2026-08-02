using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.Entities
{
    public class Course
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Título é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode ter mais de 100 caracteres")]
        public string? Title { get; set; }
        public string? Description { get; set; }

        [Required(ErrorMessage = "O Criador é obrigatório.")]
        public int CreatorId { get; set; }
        public User Creator { get; set; }

        
        public int? TeacherId { get; set; }
        public User? Teacher { get; set; }




        // relationships
        public ICollection<CourseEnrollment> EnrolledUsers { get; set; } = new List<CourseEnrollment>();
        public ICollection<CourseModule> Modules { get; set; } 

        public ICollection<Payment> Payments { get; set; }




        public Course() { }

        public Course(int id, string title, string description, int creatorId)
        {
            Id = id;
            Title = title;
            Description = description;
            CreatorId = creatorId;
        }
    }
}
