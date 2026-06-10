using Microsoft.AspNetCore.Identity;

namespace Dokkana.DAL.Models
{
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() { }
        public ApplicationRole(string roleName) : base(roleName) { }
    }
}
