namespace EducationalPlataform.Entities
{
    public class PaymentAudit
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        public string Action { get; set; } 
        public string Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Relacionamento
        public Payment Payment { get; set; }
    }
}
