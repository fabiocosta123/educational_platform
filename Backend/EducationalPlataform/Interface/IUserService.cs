using EducationalPlataform.Entities;

namespace EducationalPlataform.Interface
{
    public interface IUserService
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User> CreateAsync(User user);
    }
}
