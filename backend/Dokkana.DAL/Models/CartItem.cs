namespace Dokkana.DAL.Models
{
    // A single product line inside a cart. Price is read live from the Product
    // at display time, so only the quantity is stored here.
    public class CartItem
    {
        public int Id { get; set; }

        public int CartId { get; set; }
        public Cart? Cart { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public int Quantity { get; set; } = 1;
    }
}
