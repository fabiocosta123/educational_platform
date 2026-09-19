using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class AnnouncementUpdateDto
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(150, ErrorMessage = "O título deve ter no máximo 150 caracteres.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "O conteúdo é obrigatório.")]
        [StringLength(5000, ErrorMessage = "O aviso deve ter no máximo 5000 caracteres.")]
        public string Content { get; set; } = string.Empty;

        public int? CourseId { get; set; }

        public bool IsPublished { get; set; }
    }
}