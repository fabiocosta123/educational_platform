using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class CourseModuleCreateDto
    {
        [Required(ErrorMessage = " O nome do módulo é obrigatório. ")]
        [StringLength(100, ErrorMessage = "O nome do módulo pode ter até 100 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "A descrição pode ter no máximo 1000 caracteres.")]
        public string Description { get; set; }

        [Required]
        public int CourseId { get; set; }

        public int Order { get; set; }
        public bool IsPublished { get; set; }

    }
}
