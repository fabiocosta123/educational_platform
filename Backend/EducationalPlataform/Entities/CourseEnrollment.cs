namespace EducationalPlataform.Entities
{
    public class CourseEnrollment
    {
        public int UserId { get; set; }
        public User User { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        public CourseEnrollment() { }

        public CourseEnrollment(int userId, int courseId)
        {
            UserId = userId;
            CourseId = courseId;
        }
    }
}
