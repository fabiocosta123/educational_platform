namespace EducationalPlataform.DTOs
{
    public class FinanceSummaryDto
    {
        public decimal TotalReceived { get; set; }
        public decimal TotalPending { get; set; }
        public int TotalPayments { get; set; }
        public int Paid { get; set; }
        public int Pending { get; set; }
        public decimal DefaultRate { get; set; }
        public int ActiveStudents { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public string? Message { get; set; }
    }
}
