using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class LessonWatchUpdateDto
    {
        [Range(0, int.MaxValue)]
        public int LastWatchedSecond { get; set; }

        [Range(0, int.MaxValue)]
        public int DurationSeconds { get; set; }
    }
}
