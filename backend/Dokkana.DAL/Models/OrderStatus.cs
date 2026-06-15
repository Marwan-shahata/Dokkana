namespace Dokkana.DAL.Models
{
    // Lifecycle of a reservation/order. A new order starts as Reserved (the
    // shopper has a pickup code to collect in-store); the store can later mark
    // it Completed or Cancelled.
    public enum OrderStatus
    {
        Reserved = 0,
        Completed = 1,
        Cancelled = 2
    }
}
