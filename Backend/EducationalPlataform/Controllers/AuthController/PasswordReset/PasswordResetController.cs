using EducationalPlataform.DTOs;
using EducationalPlataform.Interface;
using Microsoft.AspNetCore.Mvc;

namespace EducationalPlataform.Controllers.AuthController.PasswordReset
{
    [ApiController]
    [Route("api/Auth")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IPasswordResetService _resetService;

        public PasswordResetController(IPasswordResetService resetService)
        {
            _resetService = resetService;
        }

        [HttpPost("reset-request")]
        public async Task<IActionResult> ResetRequest([FromBody] ResetRequestDto request)
        {
            var result = await _resetService.GenerateResetTokenAsync(request.Email);
            if (!result)
                return NotFound("Usuário não encontrado");

            return Ok("Email de recuperação enviado");
        }

        [HttpPost]
        public async Task<IActionResult> ResetConfirm([FromBody] ResetConfirmDto request)
        {
            var result = await _resetService.ResetPasswordAsync(request.Token, request.NewPassword);
            if (!result)
                return BadRequest("Token inválido ou expirado");

            return Ok("Senha redefinida com sucesso");
        }
    }

    
}
