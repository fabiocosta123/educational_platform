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

        [Range(0, 999999)]
        public decimal Price { get; set; }

        public int? InstallmentCount { get; set; }
    }
}
