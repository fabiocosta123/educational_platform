using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.Entities
{
    public class LessonProgress
    {
        public int Id { get; set; }

        // relacionamento 

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public int LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;

        // progresso
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }

        // segundos assistidos

        public int MaxWatchedSecond { get; set; } = 0;
        public int LastWatchedSecond { get; set; } = 0;
        public int TotalWatchedSeconds { get; set; } = 0;
        public bool Completed { get; set; } = false;


        // metrica para saber quantas vezes o usuário assistiu a aula, mesmo que não tenha completado
        public int ViewCount { get; set; } = 0;
        public DateTime LastAccessAt { get; set; } = DateTime.Now;

        //contructor

        public LessonProgress() { }

        public LessonProgress(int userId, int lessonId)
        {
            UserId = userId;
            LessonId = lessonId;

            StartedAt = DateTime.Now;
            LastAccessAt = DateTime.Now;

            MaxWatchedSecond = 0;
            LastWatchedSecond = 0;
            TotalWatchedSeconds = 0;

            ViewCount = 1;
            Completed = false;
        }
    }
}
