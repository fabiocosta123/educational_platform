using EducationalPlataform.Entities;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Data
{
    public class EducationalPlataformContext : DbContext
    {
        public EducationalPlataformContext(DbContextOptions<EducationalPlataformContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Course> Courses => Set<Course>();
        public DbSet<Lesson> Lessons => Set<Lesson>();
        public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<PaymentAudit> PaymentAudits => Set<PaymentAudit>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureLesson(modelBuilder);
            ConfigureCourse(modelBuilder);
            ConfigureCourseEnrollment(modelBuilder);
            ConfigurePayment(modelBuilder);
            ConfigurePaymentAudit(modelBuilder);
        }

        private static void ConfigureLesson(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Teacher)
                .WithMany(u => u.LessonsTaught)
                .HasForeignKey(l => l.TeacherId)
                .OnDelete(DeleteBehavior.ClientNoAction);

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigureCourse(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Creator)
                .WithMany(u => u.CoursesCreated)
                .HasForeignKey(c => c.CreatorId)
                .OnDelete(DeleteBehavior.ClientNoAction);

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Teacher)
                .WithMany()
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.ClientNoAction);
        }

        private static void ConfigureCourseEnrollment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CourseEnrollment>()
                .HasKey(e => new { e.UserId, e.CourseId });

            modelBuilder.Entity<CourseEnrollment>()
                .HasOne(e => e.User)
                .WithMany(u => u.CoursesEnrolled)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseEnrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.EnrolledUsers)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CourseEnrollment>()
                .Property(e => e.FinalGrade)
                .HasPrecision(5, 2);
        }

        private static void ConfigurePayment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .Property(p => p.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany(u => u.Payments)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Course)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private static void ConfigurePaymentAudit(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentAudit>()
                .HasOne(a => a.Payment)
                .WithMany(p => p.Audits)
                .HasForeignKey(a => a.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}