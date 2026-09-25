using EducationalPlataform.Data;
using EducationalPlataform.Entities;
using EducationalPlataform.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly EducationalPlataformContext _context;
        private readonly IEmailService _emailService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<PasswordResetService> _logger;

        public PasswordResetService(
            EducationalPlataformContext context,
            IEmailService emailService,
            IPasswordHasher<User> passwordHasher,
            ILogger<PasswordResetService> logger)
        {
            _context = context;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<PasswordResetRequestResult> GenerateResetTokenAsync(string email, string frontendBaseUrl)
        {
            var normalized = email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.UserEmail != null &&
                u.UserEmail.ToLower() == normalized);

            if (user == null)
            {
                return new PasswordResetRequestResult(null);
            }

            var existing = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.Id)
                .ToListAsync();
            if (existing.Count > 0)
            {
                _context.PasswordResetTokens.RemoveRange(existing);
            }

            var token = Guid.NewGuid().ToString("N");
            _context.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.Id,
                Token = token,
                Expiration = DateTime.UtcNow.AddHours(1)
            });
            await _context.SaveChangesAsync();

            var baseUrl = frontendBaseUrl.TrimEnd('/');
            var resetLink = $"{baseUrl}/reset-password?token={token}";

            try
            {
                await _emailService.SendAsync(
                    user.UserEmail!,
                    "Redefinição de senha — Anexa",
                    $"Use este link para definir uma nova senha (válido por 1 hora):\n{resetLink}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Não foi possível enviar e-mail de redefinição para {Email}.", user.UserEmail);
            }

            return new PasswordResetRequestResult(resetLink);
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return false;
            }

            var reset = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(r => r.Token == token && r.Expiration > DateTime.UtcNow);

            if (reset == null)
            {
                return false;
            }

            var user = await _context.Users.FindAsync(reset.UserId);
            if (user == null)
            {
                return false;
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
            _context.PasswordResetTokens.Remove(reset);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
