using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EducationalPlataform.Controllers.AuthController.Register
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthController(EducationalPlataformContext context, IConfiguration configuration, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (await _context.Users.AnyAsync(u => u.UserName == dto.UserName))
                return BadRequest(new { message = "Username already exists." });

            if (dto.Profile == 0)
                return BadRequest(new { message = "Selecione um perfil válido" });

            var user = new User
            {
                UserName = dto.UserName,
                UserEmail = dto.UserEmail,
                CPF = dto.CPF,
                BirthDate = dto.BirthDate,
                Profile = (UserProfile)dto.Profile
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully." });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == dto.UserName || u.UserEmail == dto.UserName);
            if (user == null) return Unauthorized(new { message = "Invalid credentials" });

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed) return Unauthorized(new { message = "Invalid credentials" });

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user);
            return Ok(new { token });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (!int.TryParse(userId, out var id)) return Unauthorized();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            return Ok(new
            {
                Id = user.Id,
                Name = user.UserName,
                Email = user.UserEmail,
                Role = user.Profile.ToString(),
                Profile = (int)user.Profile
            });
        }

        // Private helper to keep token creation in one place
        private string GenerateJwtToken(User user)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var now = DateTime.UtcNow;

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Profile.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("profile", ((int)user.Profile).ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = now,
                IssuedAt = now,
                Expires = now.AddHours(4),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }



        [HttpPost("register-course/{courseId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterCourse(
            int courseId,
            [FromBody] PublicCourseRegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // 1. Verifica se o curso existe

            var course = await _context.Courses
                .Include(c => c.Modules)
                    .ThenInclude(m => m.Lessons)
                .FirstOrDefaultAsync(c => c.Id == courseId);



            if (course == null)
            {
                return NotFound(new
                {
                    message = "Curso não encontrado."
                });
            }

            // 2. Verifica usuário pelo nome
            var usernameExists = await _context.Users
                .AnyAsync(u => u.UserName == dto.UserName);

            if (usernameExists)
            {
                return Conflict(new
                {
                    message = "Este nome de usuário já está cadastrado."
                });
            }

            // 3. Verifica e-mail
            var emailExists = await _context.Users
                .AnyAsync(u => u.UserEmail == dto.UserEmail);

            if (emailExists)
            {
                return Conflict(new
                {
                    message = "Este e-mail já está cadastrado."
                });
            }

            // 4. Verifica CPF
            var cpfExists = await _context.Users
                .AnyAsync(u => u.CPF == dto.CPF);

            if (cpfExists)
            {
                return Conflict(new
                {
                    message = "Este CPF já está cadastrado."
                });
            }

            // 5. Cria usuário
            var user = new User
            {
                UserName = dto.UserName,
                UserEmail = dto.UserEmail,
                CPF = dto.CPF,
                PhoneNumber = dto.PhoneNumber,
                BirthDate = dto.BirthDate,

                // IMPORTANTE:
                // Cadastro público sempre cria aluno.
                Profile = UserProfile.Student,

                Role = UserProfile.Student.ToString()
            };

            // 6. Gera hash da senha
            user.PasswordHash = _passwordHasher.HashPassword(
                user,
                dto.Password
            );

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 7. Salva usuário
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 8. Cria matrícula como Pending
                var enrollment = new CourseEnrollment(
                    user.Id,
                    courseId
                );

                enrollment.Status = "Pending";
                enrollment.ProgressPercentage = 0;
                enrollment.CompletedLessons = 0;
                enrollment.TotalLessons = course.Modules
                    .SelectMany(m => m.Lessons)
                    .Count();

                _context.CourseEnrollments.Add(enrollment);

                await _context.SaveChangesAsync();

                // 9. Confirma transação
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Cadastro realizado com sucesso.",
                    userId = user.Id,
                    enrollmentId = enrollment.Id,
                    courseId = course.Id,
                    courseTitle = course.Title,
                    enrollmentStatus = enrollment.Status
                });
            }
            catch
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "Não foi possível realizar o cadastro."
                });
            }
        }


    }
}
