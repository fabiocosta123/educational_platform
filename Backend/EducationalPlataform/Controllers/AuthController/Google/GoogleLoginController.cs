using EducationalPlataform.Data;
using EducationalPlataform.Interface;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.AuthController
{
    [ApiController]
    [Route("api/Auth/google-login")]
    public class GoogleLoginController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public GoogleLoginController(
            EducationalPlataformContext context,
            IJwtService jwtService,
            IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.IdToken))
            {
                return BadRequest(new { message = "Token Google ausente." });
            }

            try
            {
                var payload = await ValidateGoogleTokenAsync(request.IdToken);
                var email = payload.Email?.Trim();

                if (string.IsNullOrWhiteSpace(email))
                {
                    return Unauthorized(new { message = "A conta Google não retornou e-mail." });
                }

                var normalized = email.ToLowerInvariant();
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.UserEmail != null &&
                    u.UserEmail.ToLower() == normalized);

                if (user == null)
                {
                    return Unauthorized(new
                    {
                        message = "Não há conta com este e-mail Google. Atualize o e-mail do usuário no painel ou cadastre a conta antes."
                    });
                }

                var token = _jwtService.GenerateToken(user);
                return Ok(new { token });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { message = "Token Google inválido." });
            }
        }

        private async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(string idToken)
        {
            var clientId = _configuration["Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return await GoogleJsonWebSignature.ValidateAsync(idToken);
            }

            return await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });
        }
    }

    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }
}
