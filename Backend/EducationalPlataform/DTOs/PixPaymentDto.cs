namespace EducationalPlataform.DTOs
{
    public class PixPaymentDto
    {
        public class PixPaymentRequestDto
        {
          
            public string UserName { get; set; }            
            public string CourseTitle { get; set; }
            public decimal Amount { get; set; }
            public DateTime DueDate { get; set; }

        }

        public class PixPaymentResponseDto
        {
            public string QrCodeBase64 { get; set; }
            public string CopiaCola { get; set; }
            public string Status { get; set; } = "Pendente";
            public string DownloadUrl { get; set; }
            public string DownloadPdfUrl { get; set; }

        }
    }
}
