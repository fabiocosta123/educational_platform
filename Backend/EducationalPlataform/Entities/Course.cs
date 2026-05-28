namespace EducationalPlataform.Entities
{
    public class Course
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }

        public int CreatorId { get; set; }
        public User Creator { get; set; }



        // relationships
        public ICollection<CourseEnrollment> EnrolledUsers { get; set; } = new List<CourseEnrollment>();
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();


        public Course() { }

        public Course(int id, string title, string description, int creatorId)
        {
            Id = id;
            Title = title;
            Description = description;
            CreatorId = creatorId;
        }
    }
}
