using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class ForumReplyUpdateDto
    {
        [Required(ErrorMessage = "Conteúdo da resposta é obrigatório")]
        [StringLength(5000, ErrorMessage = "Resposta deve ter no máximo 5000 caracteres")]
        public string Content { get; set; } = string.Empty;
    }
}
