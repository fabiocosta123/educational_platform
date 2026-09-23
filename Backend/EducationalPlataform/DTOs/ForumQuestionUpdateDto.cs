using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class ForumQuestionUpdateDto
    {
        [Required(ErrorMessage = " O título é obrigatório")]
        [StringLength(150, ErrorMessage = "O título deve ter no máximo 150 caracteres")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "A pergunta é obrigatória")]
        [StringLength(5000, ErrorMessage = "A pergunta deve ter no máximo 5000 caracteres")]
        public string Content {  get; set; } = string.Empty;
    }
}
