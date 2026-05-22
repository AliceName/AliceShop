namespace TranThiNga_2380601420_Tuan4_LTWeb.Models
{
    public class CartIndexViewModel
    {
        public ShoppingCart Cart { get; set; } = new ShoppingCart();

        public HashSet<int> SelectedProductIds { get; set; } = new HashSet<int>();

        public IEnumerable<CartItem> SelectedItems =>
            Cart.Items.Where(i => SelectedProductIds.Contains(i.ProductId));

        public int SelectedQuantityCount => SelectedItems.Sum(i => i.Quantity);

        public decimal SelectedSubtotal => SelectedItems.Sum(i => i.Price * i.Quantity);

        public decimal SelectedShippingFee =>
            SelectedSubtotal >= 500_000m ? 0m : (SelectedItems.Any() ? 30_000m : 0m);

        public decimal SelectedGrandTotal => SelectedSubtotal + SelectedShippingFee;

        public bool IsSelected(int productId) => SelectedProductIds.Contains(productId);
    }
}
