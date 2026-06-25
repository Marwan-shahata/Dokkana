namespace Dokkana.BLL.DTOs.Store
{
    // Sets a store's commercial plan. Used by the owner's "Upgrade to Pro"
    // action and by the admin tier override. Tier is "Free" or "Pro".
    public class StoreTierUpdateDTO
    {
        public string Tier { get; set; } = "Free";
    }
}
