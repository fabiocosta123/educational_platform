using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class CourseModuleUpdateDto
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "Nome pode ter no máximo 100 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Descrição deve ter no máximo 1000 caracteres.")]
        public string? Description { get; set; }

        public int Order {  get; set; }

        public bool IsPublished { get; set; } = true;

    }
}
