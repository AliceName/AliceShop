namespace TranThiNga_2380601420_Tuan4_LTWeb.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }

        // Added for cart display
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
    }
}
