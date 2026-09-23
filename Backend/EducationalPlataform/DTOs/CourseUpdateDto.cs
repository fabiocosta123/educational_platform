using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class CourseUpdateDto
    {
        [Required(ErrorMessage = "Título é obrigatório.")]
        [StringLength(100)]
        public string? Title { get; set; }

        public string? Description { get; set; }
        public int TeacherId { get; set; }
    }
}
