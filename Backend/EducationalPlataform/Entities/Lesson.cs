using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.Entities
{
    public class Lesson
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Título é obrigatório.")]
        [StringLength(100, ErrorMessage = "O título deve ter no máximo 100 caracteres.")]
        public string? Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "O conteúdo deve ter no máximo 1000 caracteres.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "O link do video é obrigatório.")]
        public string VideoUrl { get; set; } = string.Empty;

        public string? PdfUrl { get; set; }

        public int Order { get; set; }

        public int DurationSeconds { get; set; }

        public bool IsPublished { get; set; } = true;


        // relacionamentos
        [Required]
        public int CourseModuleId { get; set; }

        public CourseModule? CourseModule { get; set; }

        [Required]
        public int TeacherId { get; set; }

        public User? Teacher { get; set; } = null!;

        public ICollection<LessonProgress> Progresses { get; set; } = new List<LessonProgress>();

        // constructors
        public Lesson()
        {
        }

        public Lesson(
            string title,
            string? description,
            string videoUrl,
            string? pdfUrl,
            int courseModuleId,
            int teacherId,
            int order)
        {
            Title = title;
            Description = description;
            VideoUrl = videoUrl;
            PdfUrl = pdfUrl;
            CourseModuleId = courseModuleId;
            TeacherId = teacherId;
            Order = order;
        }

    }
}
