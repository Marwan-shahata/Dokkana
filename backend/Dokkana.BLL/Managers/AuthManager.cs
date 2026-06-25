using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using Dokkana.BLL.DTOs.Auth;
using Dokkana.Common;
using Dokkana.DAL.Models;
using Dokkana.DAL.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Dokkana.BLL.Managers
{
    public class AuthManager : IAuthManager
    {
        private const string RefreshTokenProvider = "Dokkana";
        private const string RefreshTokenName = "RefreshTokenHash";
        private const string RefreshTokenExpiryName = "RefreshTokenExpiresAt";
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;

        public AuthManager(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IConfiguration configuration,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
        }

        public async Task<GeneralResult<AuthResponseDTO>> RegisterAsync(RegisterDTO dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser is not null)
                return GeneralResult<AuthResponseDTO>.Fail("Email is already registered.");

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
                return GeneralResult<AuthResponseDTO>.Fail(errors);
            }

            string role;
            if (!string.IsNullOrWhiteSpace(dto.StoreName))
            {
                role = "StoreOwner";

                var store = new Store
                {
                    Name = dto.StoreName,
                    OwnerId = user.Id,
                    IsApproved = false
                };

                await _unitOfWork.Stores.AddAsync(store);
                await _unitOfWork.SaveChangesAsync();

                user.StoreId = store.Id;
                await _userManager.UpdateAsync(user);
            }
            else
            {
                role = "Shopper";
            }

            await _userManager.AddToRoleAsync(user, role);

            var response = await CreateAuthResponseAsync(user, role);

            return GeneralResult<AuthResponseDTO>.Ok(response, "Registration successful.");
        }

        public async Task<GeneralResult<AuthResponseDTO>> LoginAsync(LoginDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return GeneralResult<AuthResponseDTO>.Fail("Invalid email or password.");

            if (await _userManager.IsLockedOutAsync(user))
                return GeneralResult<AuthResponseDTO>.Fail(
                    IsAdminDisabled(user)
                        ? "This account has been disabled."
                        : "Too many failed attempts. Please try again later.");

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                await _userManager.AccessFailedAsync(user);
                return GeneralResult<AuthResponseDTO>.Fail("Invalid email or password.");
            }

            if (user.AccessFailedCount > 0)
                await _userManager.ResetAccessFailedCountAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Shopper";

            var response = await CreateAuthResponseAsync(user, role);

            return GeneralResult<AuthResponseDTO>.Ok(response, "Login successful.");
        }

        public async Task<GeneralResult<AuthResponseDTO>> RefreshAsync(RefreshTokenDTO dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user is null)
                return GeneralResult<AuthResponseDTO>.Fail("Invalid refresh token.");

            if (await _userManager.IsLockedOutAsync(user))
                return GeneralResult<AuthResponseDTO>.Fail("This account is not active.");

            if (!await IsRefreshTokenValidAsync(user, dto.RefreshToken))
                return GeneralResult<AuthResponseDTO>.Fail("Invalid refresh token.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Shopper";
            var response = await CreateAuthResponseAsync(user, role);

            return GeneralResult<AuthResponseDTO>.Ok(response, "Session refreshed.");
        }

        public async Task<GeneralResult> RevokeRefreshTokenAsync(RefreshTokenDTO dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user is null) return GeneralResult.Ok("Logged out successfully.");

            if (await IsRefreshTokenValidAsync(user, dto.RefreshToken))
                await RemoveRefreshTokenAsync(user);

            return GeneralResult.Ok("Logged out successfully.");
        }

        public async Task<UserProfileDTO?> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user is null ? null : MapProfile(user);
        }

        public async Task<GeneralResult<UserProfileDTO>> UpdateProfileAsync(string userId, UserProfileUpdateDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return GeneralResult<UserProfileDTO>.Fail("User not found.");

            var firstName = dto.FirstName.Trim();
            var lastName = dto.LastName.Trim();
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                return GeneralResult<UserProfileDTO>.Fail("First name and last name are required.");

            user.FirstName = firstName;
            user.LastName = lastName;
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
            user.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
            user.Gender = string.IsNullOrWhiteSpace(dto.Gender) ? null : dto.Gender.Trim();
            user.Location = string.IsNullOrWhiteSpace(dto.Location) ? null : dto.Location.Trim();
            user.AvatarUrl = string.IsNullOrWhiteSpace(dto.AvatarUrl) ? null : dto.AvatarUrl.Trim();

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded
                ? GeneralResult<UserProfileDTO>.Ok(MapProfile(user), "Profile updated.")
                : GeneralResult<UserProfileDTO>.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        public async Task<GeneralResult> ChangePasswordAsync(string userId, ChangePasswordDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return GeneralResult.Fail("User not found.");

            if (string.Equals(dto.CurrentPassword, dto.NewPassword, StringComparison.Ordinal))
                return GeneralResult.Fail("New password must be different from the current password.");

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
                return GeneralResult.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));

            await RemoveRefreshTokenAsync(user);
            return GeneralResult.Ok("Password updated.");
        }

        private async Task<AuthResponseDTO> CreateAuthResponseAsync(ApplicationUser user, string role)
        {
            var (token, tokenExpiresAt) = GenerateJwtToken(user, role);
            var (refreshToken, refreshTokenExpiresAt) = await RotateRefreshTokenAsync(user);
            Store? store = null;
            if (user.StoreId.HasValue)
                store = await _unitOfWork.Stores.GetByIdAsync(user.StoreId.Value);

            if (store is null && string.Equals(role, "StoreOwner", StringComparison.OrdinalIgnoreCase))
            {
                store = (await _unitOfWork.Stores.FindAsync(s => s.OwnerId == user.Id)).FirstOrDefault();
                if (store is not null && user.StoreId != store.Id)
                {
                    user.StoreId = store.Id;
                    await _userManager.UpdateAsync(user);
                }
            }

            return new AuthResponseDTO
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = role,
                Token = token,
                TokenExpiresAt = tokenExpiresAt,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshTokenExpiresAt,
                StoreId = store?.Id ?? user.StoreId,
                StoreName = store?.Name
            };
        }

        private (string Token, DateTime ExpiresAt) GenerateJwtToken(ApplicationUser user, string role)
        {
            var jwtKey = _configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? "dokkana-api";
            var audience = _configuration["Jwt:Audience"] ?? "dokkana-client";
            var accessTokenMinutes = _configuration.GetValue("Jwt:AccessTokenMinutes", 120);
            var expiresAt = DateTime.UtcNow.AddMinutes(Math.Clamp(accessTokenMinutes, 5, 24 * 60));

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName!),
                new(ClaimTypes.Email, user.Email!),
                new(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        private static bool IsAdminDisabled(ApplicationUser user) =>
            user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow.AddYears(1);

        private async Task<(string Token, DateTime ExpiresAt)> RotateRefreshTokenAsync(ApplicationUser user)
        {
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenDays = _configuration.GetValue("Jwt:RefreshTokenDays", 14);
            var expiresAt = DateTime.UtcNow.AddDays(Math.Clamp(refreshTokenDays, 1, 90));

            await _userManager.SetAuthenticationTokenAsync(
                user,
                RefreshTokenProvider,
                RefreshTokenName,
                HashRefreshToken(refreshToken));

            await _userManager.SetAuthenticationTokenAsync(
                user,
                RefreshTokenProvider,
                RefreshTokenExpiryName,
                expiresAt.ToString("O"));

            return (refreshToken, expiresAt);
        }

        private async Task<bool> IsRefreshTokenValidAsync(ApplicationUser user, string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return false;

            var storedHash = await _userManager.GetAuthenticationTokenAsync(
                user,
                RefreshTokenProvider,
                RefreshTokenName);
            var storedExpiry = await _userManager.GetAuthenticationTokenAsync(
                user,
                RefreshTokenProvider,
                RefreshTokenExpiryName);

            if (string.IsNullOrWhiteSpace(storedHash) ||
                string.IsNullOrWhiteSpace(storedExpiry) ||
                !DateTimeOffset.TryParse(storedExpiry, out var expiresAt) ||
                expiresAt <= DateTimeOffset.UtcNow)
            {
                await RemoveRefreshTokenAsync(user);
                return false;
            }

            var candidateHash = HashRefreshToken(refreshToken);
            return string.Equals(storedHash, candidateHash, StringComparison.Ordinal);
        }

        private async Task RemoveRefreshTokenAsync(ApplicationUser user)
        {
            await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenName);
            await _userManager.RemoveAuthenticationTokenAsync(user, RefreshTokenProvider, RefreshTokenExpiryName);
        }

        private static string HashRefreshToken(string refreshToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        private static UserProfileDTO MapProfile(ApplicationUser user) => new()
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            Gender = user.Gender,
            Location = user.Location,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt
        };
    }
}
