using EducationalPlataform.Data;
using EducationalPlataform.Entities;
using EducationalPlataform.Interface;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Services
{
    public class UserService : IUserService
    {
        private readonly EducationalPlataformContext _context;
        public UserService(EducationalPlataformContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserEmail == email);
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
    }
}
