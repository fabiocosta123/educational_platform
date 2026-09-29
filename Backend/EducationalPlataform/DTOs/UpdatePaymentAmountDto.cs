using System.ComponentModel.DataAnnotations;

namespace EducationalPlataform.DTOs
{
    public class UpdatePaymentAmountDto
    {
        [Range(0.01, 999999)]
        public decimal Amount { get; set; }
    }
}
