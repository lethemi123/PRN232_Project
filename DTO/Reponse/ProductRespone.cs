namespace ProjectPRN232.DTO.Reponse
{
    public class ProductRespone
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
      
    }
}
