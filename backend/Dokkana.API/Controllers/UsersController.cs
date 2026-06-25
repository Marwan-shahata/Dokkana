using Dokkana.BLL.DTOs.User;
using Dokkana.BLL.Managers;
using Dokkana.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class UsersController : ControllerBase
    {
        private readonly IUserAdminManager _users;
        private readonly IAuditLogManager _audit;

        public UsersController(IUserAdminManager users, IAuditLogManager audit)
        {
            _users = users;
            _audit = audit;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _users.GetAllAsync();
            return Ok(GeneralResult<IEnumerable<UserAdminDTO>>.Ok(users));
        }

        // Enable or disable a user account.
        [HttpPut("{id}/status")]
        public async Task<IActionResult> SetStatus(string id, SetUserStatusDTO dto)
        {
            var result = await _users.SetDisabledAsync(id, dto.Disabled);
            if (!result.Success) return BadRequest(result);

            await _audit.LogAsync(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.FindFirstValue(ClaimTypes.Email),
                dto.Disabled ? "DisableUser" : "EnableUser",
                "User",
                id);

            return Ok(result);
        }
    }
}
