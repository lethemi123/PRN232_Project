namespace ProjectPRN232.DTO.Reponse
{
    public class VariantRespone
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string VariantName { get; set; } = null!; // ví dụ: 128GB, 256GB
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string ImageUrl { get; set; } = null!;

    }
}
