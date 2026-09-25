using EducationalPlataform.DTOs;
using EducationalPlataform.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EducationalPlataform.Controllers.AuthController.PasswordReset
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/Auth")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IPasswordResetService _resetService;
        private readonly IConfiguration _configuration;

        public PasswordResetController(IPasswordResetService resetService, IConfiguration configuration)
        {
            _resetService = resetService;
            _configuration = configuration;
        }

        [HttpPost("reset-request")]
        public async Task<IActionResult> ResetRequest([FromBody] ResetRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email))
            {
                return BadRequest(new { message = "Informe o e-mail." });
            }

            var origin = Request.Headers.Origin.FirstOrDefault()
                ?? _configuration["Frontend:PublicUrl"]
                ?? "http://localhost:3000";

            var result = await _resetService.GenerateResetTokenAsync(request.Email, origin);
            return Ok(new
            {
                message = "Se o e-mail estiver cadastrado, o link de redefinição foi gerado.",
                resetLink = result.ResetLink
            });
        }

        [HttpPost("reset-confirm")]
        public async Task<IActionResult> ResetConfirm([FromBody] ResetConfirmDto request)
        {
            var result = await _resetService.ResetPasswordAsync(request.Token, request.NewPassword);
            if (!result)
            {
                return BadRequest(new { message = "Token inválido ou expirado. A senha precisa ter no mínimo 6 caracteres." });
            }

            return Ok(new { message = "Senha redefinida com sucesso." });
        }
    }
}
