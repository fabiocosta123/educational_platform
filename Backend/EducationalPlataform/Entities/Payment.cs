using EducationalPlataform.Models.Enums;

namespace EducationalPlataform.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending; 
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? PaidAt { get; set; }
        public DateTime? DueDate { get; set; }


        //Relacionamentos
        public User User { get; set; }
        public Course Course { get; set; }
        public ICollection<PaymentAudit> Audits { get; set; }

    }
}
