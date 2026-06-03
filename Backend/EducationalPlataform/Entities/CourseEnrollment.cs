namespace EducationalPlataform.Entities
{
    public class CourseEnrollment
    {
        public int Id { get; set; }  

        public int UserId { get; set; }
        public User User { get; set; }

        public int CourseId { get; set; }
        public Course Course { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public decimal? FinalGrade { get; set; }
        public int ProgressPercentage { get; set; }
        public string Status { get; set; } = "Ativo";

        public CourseEnrollment() { }

        public CourseEnrollment(int userId, int courseId)
        {
            UserId = userId;
            CourseId = courseId;
            Status = "Ativo";
            ProgressPercentage = 0;
        }
    }
}
