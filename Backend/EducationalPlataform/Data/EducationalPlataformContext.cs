using EducationalPlataform.Entities;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Data
{
    public class EducationalPlataformContext : DbContext
    {
        public EducationalPlataformContext(DbContextOptions<EducationalPlataformContext> options)
            : base(options)
        { }

        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentAudit> PaymentAudits { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relationship Lesson → Teacher
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Teacher)
                .WithMany(u => u.LessonsTaught)
                .HasForeignKey(l => l.TeacherId)
                .OnDelete(DeleteBehavior.ClientNoAction);

            // Relationship Course → Creator
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Creator)
                .WithMany(u => u.CoursesCreated)
                .HasForeignKey(c => c.CreatorId)
                .OnDelete(DeleteBehavior.ClientNoAction);

            // Relationship Course → Teacher
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Teacher)
                .WithMany() // ou .WithMany(u => u.CoursesTaught) se quiser relação inversa
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.ClientNoAction);

            // Relationship CourseEnrollment (N:N User ↔ Course)
            modelBuilder.Entity<CourseEnrollment>()
                .HasKey(ce => new { ce.UserId, ce.CourseId });

            modelBuilder.Entity<CourseEnrollment>()
                .HasOne(ce => ce.User)
                .WithMany(u => u.CoursesEnrolled)
                .HasForeignKey(ce => ce.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseEnrollment>()
                .HasOne(ce => ce.Course)
                .WithMany(c => c.EnrolledUsers)
                .HasForeignKey(ce => ce.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseEnrollment>()
                .Property(e => e.FinalGrade)
                .HasPrecision(5, 2);

            // Relationship Lesson → Course
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship Payment -> User
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship Payment → Course
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Course)
                .WithMany(c => c.Payments) 
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship Payment Audit
            modelBuilder.Entity<PaymentAudit>()
                .HasOne(a => a.Payment)
                .WithMany(p => p.Audits) 
                .HasForeignKey(a => a.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
