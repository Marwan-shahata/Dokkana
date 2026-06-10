using Microsoft.AspNetCore.Identity;

namespace Dokkana.DAL.Models
{
    // Extends Identity's user. A StoreOwner is linked to one Store.
    public class ApplicationUser : IdentityUser
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }

        // Set only when this user owns a store (role = StoreOwner).
        public int? StoreId { get; set; }
        public Store? Store { get; set; }
    }
}
