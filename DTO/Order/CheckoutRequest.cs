namespace ProjectPRN232.DTO.Order
{
    public class CheckoutRequest
    {
        public List<int> CartItemIds { get; set; } = new();
        public string ReceiverName { get; set; } = null!; 
        public string ReceiverPhone { get; set; } = null!;
        public string ReceiverAddress { get; set; } = null!;
        public string? PaymentMethod { get; set; }
    }
}
