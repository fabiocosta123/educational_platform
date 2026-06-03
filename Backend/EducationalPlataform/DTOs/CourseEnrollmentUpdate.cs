namespace EducationalPlataform.DTOs
{
    public class CourseEnrollmentUpdateDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public decimal? FinalGrade { get; set; }

        // Percentual de progresso (0 a 100)
        public int ProgressPercentage { get; set; }

        // Status da inscrição: Ativo, Concluído, Cancelado
        public string Status { get; set; } = "Ativo";
    }
}
