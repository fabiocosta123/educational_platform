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

        public DbSet<CourseModule> CourseModules => Set<CourseModule>();
        public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();

        public DbSet<ForumQuestion> ForumQuestions => Set<ForumQuestion>();       
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<ForumReply> ForumReplies => Set<ForumReply>();
        public DbSet<Assessment> Assessments => Set<Assessment>();
        public DbSet<AssessmentQuestion> AssessmentQuestions => Set<AssessmentQuestion>();
        public DbSet<AssessmentOption> AssessmentOptions => Set<AssessmentOption>();
        public DbSet<AssessmentAttempt> AssessmentAttempts => Set<AssessmentAttempt>();
        public DbSet<AssessmentAnswer> AssessmentAnswers => Set<AssessmentAnswer>();
        public DbSet<Certificate> Certificates => Set<Certificate>();
        public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureLesson(modelBuilder);
            ConfigureCourse(modelBuilder);
            ConfigureCourseEnrollment(modelBuilder);
            ConfigureLessonProgress(modelBuilder);
            ConfigurePayment(modelBuilder);
            ConfigurePaymentAudit(modelBuilder);
            ConfigureForum(modelBuilder);
            ConfigureAnnouncement(modelBuilder);
            ConfigureAssessments(modelBuilder);
            ConfigureCertificate(modelBuilder);
            ConfigurePasswordResetToken(modelBuilder);
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
                
                // Tabela                

                entity.ToTable("LessonProgress");

                entity.HasKey(lp => lp.Id);

                
                // Índices
                

                // Um usuário só pode possuir um progresso por aula.
                entity.HasIndex(lp => new
                {
                    lp.UserId,
                    lp.LessonId
                })
                .IsUnique();

                
                // Relacionamento Usuário
                

                entity.HasOne(lp => lp.User)
                    .WithMany()
                    .HasForeignKey(lp => lp.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                
                // Relacionamento Aula
                

                entity.HasOne(lp => lp.Lesson)
                    .WithMany(l => l.Progresses)
                    .HasForeignKey(lp => lp.LessonId)
                    .OnDelete(DeleteBehavior.Cascade);

                
                // Valores padrão
                

                entity.Property(lp => lp.StartedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

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
                .HasKey(e => e.Id);

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

            modelBuilder.Entity<CourseEnrollment>()
                .HasIndex(e => new { e.UserId, e.CourseId })
                .IsUnique();
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

        private static void ConfigureForum(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ForumQuestion>()
                .HasOne(q => q.User)
                .WithMany()
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ForumQuestion>()
                .HasOne(q => q.Course)
                .WithMany()
                .HasForeignKey(q => q.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ForumQuestion>()
                .HasOne(q => q.Lesson)
                .WithMany()
                .HasForeignKey(q => q.LessonId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ForumReply>()
                .HasOne(r => r.ForumQuestion)
                .WithMany(q => q.Replies)
                .HasForeignKey(r => r.ForumQuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ForumReply>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigureAnnouncement(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.Author)
                .WithMany(u => u.AnnouncementsCreated)
                .HasForeignKey(a => a.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.Course)
                .WithMany(c => c.Announcements)
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        private static void ConfigureAssessments(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Assessment>(entity =>
            {
                entity.Property(a => a.Type).HasConversion<string>();
                entity.Property(a => a.PassingScore).HasPrecision(5, 2);
                entity.HasOne(a => a.Course)
                    .WithMany(c => c.Assessments)
                    .HasForeignKey(a => a.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AssessmentQuestion>(entity =>
            {
                entity.Property(q => q.Points).HasPrecision(5, 2);
                entity.HasOne(q => q.Assessment)
                    .WithMany(a => a.Questions)
                    .HasForeignKey(q => q.AssessmentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AssessmentOption>(entity =>
            {
                entity.HasOne(o => o.Question)
                    .WithMany(q => q.Options)
                    .HasForeignKey(o => o.QuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AssessmentAttempt>(entity =>
            {
                entity.Property(a => a.Score).HasPrecision(5, 2);
                entity.Property(a => a.Status).HasMaxLength(20);
                entity.HasOne(a => a.Assessment)
                    .WithMany(a => a.Attempts)
                    .HasForeignKey(a => a.AssessmentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AssessmentAnswer>(entity =>
            {
                entity.HasOne(a => a.Attempt)
                    .WithMany(t => t.Answers)
                    .HasForeignKey(a => a.AttemptId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigureCertificate(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Certificate>(entity =>
            {
                entity.Property(c => c.ExamAverage).HasPrecision(5, 2);
                entity.HasIndex(c => c.Code).IsUnique();
                entity.HasIndex(c => new { c.UserId, c.CourseId }).IsUnique();
                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(c => c.Course)
                    .WithMany(c => c.Certificates)
                    .HasForeignKey(c => c.CourseId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void ConfigurePasswordResetToken(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.ToTable("PasswordResetTokens");
                entity.HasIndex(t => t.Token).IsUnique();
                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}