using EducationalPlataform.Data;
using EducationalPlataform.Entities;
using EducationalPlataform.Interface;
using Microsoft.EntityFrameworkCore;


namespace EducationalPlataform.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public PasswordResetService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<bool> GenerateResetTokenAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserEmail == email);
            if (user == null) return false;

            var token = Guid.NewGuid().ToString();
            var reset = new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                Expiration = DateTime.UtcNow.AddHours(1)
            };

            _context.PasswordResetTokens.Add(reset);
            await _context.SaveChangesAsync();

            await _emailService.SendAsync(email, "Recuperação de senha", $"Clique no link para redefinir sua senha: https://frontend/reset-password?token={token}");

            return true;
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var reset = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(r => r.Token == token && r.Expiration > DateTime.UtcNow);

            if (reset == null) return false;

            var user = await _context.Users.FindAsync(reset.UserId);
            if (user == null) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.PasswordResetTokens.Remove(reset);
            await _context.SaveChangesAsync();
            
            return true;
        }
    }
}
