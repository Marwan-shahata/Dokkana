namespace Dokkana.BLL.DTOs.User
{
    // Admin view of a platform user.
    public class UserAdminDTO
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Shopper";
        public bool IsDisabled { get; set; }
    }

    public class SetUserStatusDTO
    {
        public bool Disabled { get; set; }
    }
}
