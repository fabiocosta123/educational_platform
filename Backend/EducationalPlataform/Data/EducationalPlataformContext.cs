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
                .HasOne(l => l.CourseModule)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseModuleId)
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
               .WithMany(u => u.CoursesTaught)
               .HasForeignKey(c => c.TeacherId)
               .OnDelete(DeleteBehavior.ClientNoAction);
        }

        public static void ConfigureLessonProgress(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LessonProgress>(entity =>
            {
                // ==========================
                // Tabela
                // ==========================

                entity.ToTable("LessonProgress");

                entity.HasKey(lp => lp.Id);

                // ==========================
                // Índices
                // ==========================

                // Um usuário só pode possuir um progresso por aula.
                entity.HasIndex(lp => new
                {
                    lp.UserId,
                    lp.LessonId
                })
                .IsUnique();

                // ==========================
                // Relacionamento Usuário
                // ==========================

                entity.HasOne(lp => lp.User)
                    .WithMany()
                    .HasForeignKey(lp => lp.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ==========================
                // Relacionamento Aula
                // ==========================

                entity.HasOne(lp => lp.Lesson)
                    .WithMany(l => l.Progresses)
                    .HasForeignKey(lp => lp.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ==========================
                // Valores padrão
                // ==========================

                entity.Property(lp => lp.StartedAt)
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(lp => lp.LastWatchedSecond)
                    .HasDefaultValue(0);

                entity.Property(lp => lp.MaxWatchedSecond)
                    .HasDefaultValue(0);

                entity.Property(lp => lp.TotalWatchedSeconds)
                    .HasDefaultValue(0);

                entity.Property(lp => lp.Completed)
                    .HasDefaultValue(false);
            });
        }

        private static void ConfigureCourseEnrollment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CourseEnrollment>()
                .HasKey(e => new { e.UserId, e.CourseId });

            modelBuilder.Entity<CourseEnrollment>()
                .HasOne(e => e.User)
                .WithMany(u => u.CourseEnrollments)
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

            modelBuilder.Entity<Payment>()
               .Property(p => p.Amount)
               .HasPrecision(10, 2);
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