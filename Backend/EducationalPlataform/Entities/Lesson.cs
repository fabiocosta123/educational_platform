using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.Entities
{
    public class Lesson
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Título é obrigatório.")]
        [StringLength(100, ErrorMessage = "O título deve ter no máximo 100 caracteres.")]
        public string? Title { get; set; }
        public string? Content { get; set; }
        public DateTime Date { get; set; }


        // relationships
        [Required(ErrorMessage = "O curso é obrigatório.")]
        public int CourseId { get; set; }
        public Course Course { get; set; }

        public int TeacherId { get; set; }
        public User Teacher { get; set; }


        // constructors
        public Lesson() { }

        public Lesson(string? title, string? content, DateTime date, int courseId, int teacherId)
        {
            
            Title = title;
            Content = content;
            Date = date;
            CourseId = courseId;
            TeacherId = teacherId;
            
        }

    }
}
