using EducationalPlataform.Data;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Services
{
    public class CertificateEligibilityService
    {
        private readonly EducationalPlataformContext _context;

        public CertificateEligibilityService(EducationalPlataformContext context)
        {
            _context = context;
        }

        public async Task<(bool Eligible, string Reason, decimal ExamAverage)> EvaluateAsync(int userId, int courseId)
        {
            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);

            if (enrollment == null)
                return (false, "Você não está matriculado neste curso.", 0);

            if (enrollment.ProgressPercentage < 100)
                return (false, "Conclua todas as aulas do curso (100%) para liberar o certificado.", 0);

            var exams = await _context.Assessments
                .Where(a => a.CourseId == courseId && a.IsPublished && a.Type == AssessmentType.Exam)
                .Select(a => a.Id)
                .ToListAsync();

            if (exams.Count == 0)
                return (false, "Este curso ainda não possui provas. O certificado exige média de 70% nas provas.", 0);

            var attempts = await _context.AssessmentAttempts
                .Where(t => t.UserId == userId && exams.Contains(t.AssessmentId))
                .ToListAsync();

            var bestByExam = exams
                .Select(examId => attempts.Where(t => t.AssessmentId == examId).Select(t => (decimal?)t.Score).DefaultIfEmpty().Max())
                .ToList();

            if (bestByExam.Any(score => score == null))
                return (false, "Realize todas as provas do curso. O certificado exige média de 70%.", 0);

            var average = bestByExam.Average(score => score!.Value);
            if (average < 70)
                return (false, $"A média nas provas é {average:0.#}%. É necessário 70% ou mais.", average);

            return (true, "Requisitos cumpridos.", average);
        }
    }
}
