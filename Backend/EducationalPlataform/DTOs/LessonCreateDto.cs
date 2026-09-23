using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class LessonCreateDto
    {
        [Required(ErrorMessage = "O título da aula é obrigatório.")]
        [StringLength(100, ErrorMessage = "O título deve ter no máximo 100 caracteres.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "A URL do vídeo é obrigatória.")]
        public string VideoUrl { get; set; } = string.Empty;

        public string? PdfUrl { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A ordem da aula deve ser maior que zero.")]
        public int Order { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "A duração do vídeo é obrigatória e deve ser maior que zero.")]
        public int DurationSeconds { get; set; }

        public bool IsPublished { get; set; } = true;

        [Range(1, int.MaxValue, ErrorMessage = "O módulo da aula é obrigatório.")]
        public int CourseModuleId { get; set; }
    }
}
