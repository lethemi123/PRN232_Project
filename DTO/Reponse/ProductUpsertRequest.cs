namespace ProjectPRN232.DTO.Reponse
{
    public class ProductUpsertRequest
    {
        public string ProductName { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? CategoryId { get; set; }
    }
}
