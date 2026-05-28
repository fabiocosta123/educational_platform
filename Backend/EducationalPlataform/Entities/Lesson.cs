namespace EducationalPlataform.Entities
{
    public class Lesson
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public DateTime Date { get; set; }


        // relationships
        public int CourseId { get; set; }
        public Course Course { get; set; }


        // constructors
        public Lesson() { }

        public Lesson(int id, string? title, string? content, DateTime date, int courseId)
        {
            Id = id;
            Title = title;
            Content = content;
            Date = date;
            CourseId = courseId;
        }

    }
}
