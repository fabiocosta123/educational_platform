using EducationalPlataform.Entities;

namespace EducationalPlataform.Interface
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
