namespace EducationalPlataform.DTOs
{
    public class LessonProgressReadDto
    {
        public int LessonId { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int LastWatchedSecond { get; set; }
        public int MaxWatchedSecond { get; set; }
    }
}

