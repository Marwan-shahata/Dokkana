using System.IdentityModel.Tokens.Jwt;
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

            var token = await GenerateJwtTokenAsync(user, role);

            var response = new AuthResponseDTO
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = role,
                Token = token,
                StoreId = user.StoreId,
                StoreName = dto.StoreName
            };

            return GeneralResult<AuthResponseDTO>.Ok(response, "Registration successful.");
        }

        public async Task<GeneralResult<AuthResponseDTO>> LoginAsync(LoginDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return GeneralResult<AuthResponseDTO>.Fail("Invalid email or password.");

            var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
                return GeneralResult<AuthResponseDTO>.Fail("Invalid email or password.");

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Shopper";

            var token = await GenerateJwtTokenAsync(user, role);
            var store = await _unitOfWork.Stores.GetByIdAsync(user.StoreId ?? 0);

            var response = new AuthResponseDTO
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = role,
                Token = token,
                StoreId = user.StoreId,
                StoreName = store?.Name
            };

            return GeneralResult<AuthResponseDTO>.Ok(response, "Login successful.");
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user, string role)
        {
            var jwtKey = _configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? "dokkana-api";
            var audience = _configuration["Jwt:Audience"] ?? "dokkana-client";

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
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
