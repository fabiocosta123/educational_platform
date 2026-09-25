using EducationalPlataform.Data;
using EducationalPlataform.DTOs;
using EducationalPlataform.Entities;
using EducationalPlataform.Models.Enums;
using EducationalPlataform.Validation;
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

            if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
                return BadRequest(new { message = "Nome e senha (mínimo 6 caracteres) são obrigatórios." });

            var email = dto.UserEmail?.Trim() ?? "";
            if (await _context.Users.AnyAsync(u => u.UserName == dto.UserName.Trim()))
                return BadRequest(new { message = "Este nome de usuário já existe." });

            if (!string.IsNullOrWhiteSpace(email)
                && await _context.Users.AnyAsync(u => u.UserEmail != null && u.UserEmail.ToLower() == email.ToLower()))
                return BadRequest(new { message = "Este e-mail já está cadastrado. Faça login." });

            var user = new User
            {
                UserName = dto.UserName.Trim(),
                UserEmail = email,
                CPF = string.IsNullOrWhiteSpace(dto.CPF) ? dto.CPF : CpfValidator.Format(dto.CPF),
                PhoneNumber = dto.PhoneNumber,
                BirthDate = dto.BirthDate,
                Profile = UserProfile.Student,
                Role = nameof(UserProfile.Student)
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cadastro realizado com sucesso." });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == dto.UserName || u.UserEmail == dto.UserName);
            if (user == null) return Unauthorized(new { message = "Invalid credentials" });

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                return Unauthorized(new
                {
                    message = "Esta conta ainda não possui senha. Defina uma senha no cadastro do aluno."
                });
            }

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

            var displayName = user.UserName ?? user.UserEmail ?? user.Id.ToString();

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, displayName),
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

            var formattedCpf = CpfValidator.Format(dto.CPF);
            var email = dto.UserEmail.Trim();

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

            // 2. E-mail já cadastrado: orientar login em vez de criar outra conta
            var existingByEmail = await _context.Users
                .Include(u => u.CourseEnrollments)
                .FirstOrDefaultAsync(u =>
                    u.UserEmail != null &&
                    u.UserEmail.ToLower() == email.ToLower());

            if (existingByEmail != null)
            {
                var alreadyEnrolled = existingByEmail.CourseEnrollments
                    .Any(e => e.CourseId == courseId);

                if (alreadyEnrolled)
                {
                    return Conflict(new
                    {
                        message = "Você já está inscrito neste curso. Faça login para acessá-lo."
                    });
                }

                return Conflict(new
                {
                    message = "Este e-mail já possui uma conta. Faça login para se inscrever neste curso."
                });
            }

            // 3. CPF já cadastrado (com ou sem máscara)
            var usersWithCpf = await _context.Users
                .Where(u => u.CPF != null && u.CPF != "")
                .Select(u => u.CPF)
                .ToListAsync();

            var cpfExists = usersWithCpf.Any(stored =>
                CpfValidator.SameCpf(stored, formattedCpf));

            if (cpfExists)
            {
                return Conflict(new
                {
                    message = "Este CPF já está cadastrado. Faça login para se inscrever neste curso."
                });
            }

            // 5. Cria usuário
            var user = new User
            {
                UserName = dto.UserName,
                UserEmail = dto.UserEmail,
                CPF = formattedCpf,
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

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

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


    }
}
