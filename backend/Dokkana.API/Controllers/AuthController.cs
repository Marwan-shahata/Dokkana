using Dokkana.BLL.DTOs.Auth;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthManager _authManager;

        public AuthController(IAuthManager authManager)
        {
            _authManager = authManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO dto)
        {
            var result = await _authManager.RegisterAsync(dto);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginDTO dto)
        {
            var result = await _authManager.LoginAsync(dto);
            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenDTO dto)
        {
            var result = await _authManager.RefreshAsync(dto);
            return result.Success ? Ok(result) : Unauthorized(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenDTO? dto)
        {
            if (dto is not null &&
                !string.IsNullOrWhiteSpace(dto.UserId) &&
                !string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                await _authManager.RevokeRefreshTokenAsync(dto);
            }

            return Ok(GeneralResult.Ok("Logged out successfully."));
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized(GeneralResult.Fail("Missing user."));

            var profile = await _authManager.GetProfileAsync(userId);
            return profile is null
                ? NotFound(GeneralResult.Fail(Errors.NotFound))
                : Ok(GeneralResult<UserProfileDTO>.Ok(profile));
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe(UserProfileUpdateDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized(GeneralResult.Fail("Missing user."));

            var result = await _authManager.UpdateProfileAsync(userId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("me/password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized(GeneralResult.Fail("Missing user."));

            var result = await _authManager.ChangePasswordAsync(userId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
