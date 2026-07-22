namespace EducationalPlataform.DTOs
{
    public class PixWebhookDto
    {
        public int PaymentId { get; set; }
        public string TransactionId { get; set; }
        public string Status { get; set; } 
        public decimal Amount { get; set; }
    }
}
