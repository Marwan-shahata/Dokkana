using Dokkana.BLL.DTOs.User;
using Dokkana.Common;
using Dokkana.DAL.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dokkana.BLL.Managers
{
    public class UserAdminManager : IUserAdminManager
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserAdminManager(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IEnumerable<UserAdminDTO>> GetAllAsync()
        {
            var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
            var result = new List<UserAdminDTO>(users.Count);

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                result.Add(new UserAdminDTO
                {
                    Id = u.Id,
                    Name = $"{u.FirstName} {u.LastName}".Trim(),
                    Email = u.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "Shopper",
                    // A user is "disabled" when locked out into the future.
                    IsDisabled = u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow,
                    CreatedAt = u.CreatedAt
                });
            }

            return result;
        }

        public async Task<GeneralResult> SetDisabledAsync(string userId, bool disabled)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return GeneralResult.Fail("User not found.");

            // Never let an admin account be disabled (would risk lockout of the platform).
            if (disabled && await _userManager.IsInRoleAsync(user, "Admin"))
                return GeneralResult.Fail("Admin accounts cannot be disabled.");

            await _userManager.SetLockoutEnabledAsync(user, true);
            var until = disabled ? DateTimeOffset.UtcNow.AddYears(100) : (DateTimeOffset?)null;
            var res = await _userManager.SetLockoutEndDateAsync(user, until);

            return res.Succeeded
                ? GeneralResult.Ok(disabled ? "User disabled." : "User enabled.")
                : GeneralResult.Fail("Could not update user status.");
        }
    }
}
