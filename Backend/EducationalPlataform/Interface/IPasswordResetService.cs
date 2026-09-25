namespace EducationalPlataform.Interface
{
    public record PasswordResetRequestResult(string? ResetLink);

    public interface IPasswordResetService
    {
        Task<PasswordResetRequestResult> GenerateResetTokenAsync(string email, string frontendBaseUrl);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }
}
