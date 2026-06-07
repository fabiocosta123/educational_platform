using EducationalPlataform.Data;
using EducationalPlataform.Entities;
using EducationalPlataform.Interface;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth;


namespace EducationalPlataform.AuthController
{
    [ApiController]
    [Route("api/Auth/google-login")]
    public class GoogleLoginController : ControllerBase
    {
        private readonly EducationalPlataformContext _context;
        private readonly IJwtService _jwtService;

        public GoogleLoginController(EducationalPlataformContext context, IJwtService jwtService)
        {
            _context = context;           
            _jwtService = jwtService;
        }

        [HttpPost]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);

                var email = payload.Email;
                var name = payload.Name;

                var user = _context.Users.FirstOrDefault(u=> u.UserEmail == email);
                if(user == null)
                {
                    user = new User
                    {
                        UserName = name,
                        UserEmail = email,
                        Profile = 0
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                var token = _jwtService.GenerateToken(user);

                return Ok(new { token });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized("Token Google inválido");
            }
        }
    }

    public class GoogleLoginRequest
    {
        public string IdToken { get; set; }
    }
}
