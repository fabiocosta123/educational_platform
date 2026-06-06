namespace EducationalPlataform.Interface
{
    public interface IPasswordResetService
    {
        Task<bool> GenerateResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }
}
