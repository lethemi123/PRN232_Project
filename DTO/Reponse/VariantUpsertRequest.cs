namespace ProjectPRN232.DTO.Reponse
{
    public class VariantUpsertRequest
    {
        public int ProductId { get; set; }
        public string VariantName { get; set; } = null!;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }
    }
}
