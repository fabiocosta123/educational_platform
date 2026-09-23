using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class ForumReplyCreateDto
    {
        [Required(ErrorMessage = "O conteúdo da resposta é obrigatório")]
        [StringLength(5000, ErrorMessage = " A resposta deve ter no máximo 5000 caracteres")]
        public string Content { get; set; } = string.Empty;
    }
}
