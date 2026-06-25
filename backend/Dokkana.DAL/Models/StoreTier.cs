namespace Dokkana.DAL.Models
{
    // A store's commercial plan. Free is the default; Pro unlocks promoted
    // placement, analytics, and the "Pro" badge shown to shoppers.
    public enum StoreTier
    {
        Free = 0,
        Pro = 1
    }
}
