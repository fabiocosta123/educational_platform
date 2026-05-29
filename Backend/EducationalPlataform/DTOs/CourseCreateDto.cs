using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class CourseCreateDto
    {
        [Required(ErrorMessage = "Título é obrigatório.")]
        [StringLength(100, ErrorMessage = "O título deve ter no máximo 100 caracteres.")]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "O Criador é obrigatório.")]
        public int CreatorId { get; set; }
    }
}
