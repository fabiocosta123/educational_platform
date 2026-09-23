using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class ForumQuestionCreateDto
    {
        [Required(ErrorMessage = "Título é Obrigatório")]
        [StringLength(150, ErrorMessage = " O título deve ter no máximo 150 caracteres")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pergunta obrigatória")]
        [StringLength(5000, ErrorMessage = " A pergunta deve ter no máximo 5000 caracteres")]
        public string Content {  get; set; } = string.Empty;


        [Required(ErrorMessage = "Curso é obrigatório")]
        public int CourseId { get; set; }

        public int? LessonId {  get; set; }
    }
}
