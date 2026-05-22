using System.ComponentModel.DataAnnotations;

namespace TranThiNga_2380601420_Tuan4_LTWeb.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        [Display(Name = "Họ và tên")]
        public string RecipientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố")]
        [Display(Name = "Tỉnh / Thành phố")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập quận/huyện")]
        [Display(Name = "Quận / Huyện")]
        public string District { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi tiết")]
        [Display(Name = "Địa chỉ (số nhà, đường)")]
        public string AddressLine { get; set; } = string.Empty;

        [Display(Name = "Ghi chú đơn hàng")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "cod";

        public List<int> SelectedProductIds { get; set; } = new List<int>();

        public ShoppingCart Cart { get; set; } = new ShoppingCart();

        public decimal Subtotal => Cart.Items.Sum(i => i.Price * i.Quantity);

        public decimal ShippingFee => Subtotal >= 500_000m ? 0m : 30_000m;

        public decimal GrandTotal => Subtotal + ShippingFee;

        public string BuildShippingAddress()
        {
            return $"{AddressLine}, {District}, {City}";
        }

        public string BuildOrderNotes()
        {
            var paymentLabel = PaymentMethod switch
            {
                "bank" => "Chuyển khoản ngân hàng",
                "momo" => "Ví MoMo",
                _ => "Thanh toán khi nhận hàng (COD)"
            };

            var parts = new List<string> { $"Thanh toán: {paymentLabel}", $"Người nhận: {RecipientName}", $"ĐT: {Phone}" };
            if (!string.IsNullOrWhiteSpace(Notes))
            {
                parts.Add($"Ghi chú: {Notes.Trim()}");
            }
            return string.Join(" | ", parts);
        }
    }
}
