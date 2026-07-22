namespace EducationalPlataform.DTOs
{
    public class PixPaymentDto
    {
        public class PixPaymentRequestDto
        {
            public int UserId { get; set; }
            public int CourseId { get; set; }
            public decimal Amount { get; set; }
        }

        public class PixPaymentResponseDto
        {
            public string QrCodeBase64 { get; set; }
            public string CopiaCola { get; set; }
            public string Status { get; set; } = "Pendente";
        }
    }
}
