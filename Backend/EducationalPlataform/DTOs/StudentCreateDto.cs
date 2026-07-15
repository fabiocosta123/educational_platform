namespace EducationalPlataform.DTOs
{
    public class StudentCreateDto
    {
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string CPF { get; set; }
        public DateTime BirthDate { get; set; }

        public int CourseId { get; set; }
        public int TeacherId { get; set; }
        public string Status { get; set; } = "Ativo";
    }
}
