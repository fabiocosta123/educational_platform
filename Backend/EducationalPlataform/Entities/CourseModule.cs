namespace EducationalPlataform.Entities
{
    public class CourseModule
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public int Order { get; set; }
        public bool IsPublished { get; set; }
        public int CourseId { get; set; }

        public Course Course { get; set; }
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    } 
    
}
