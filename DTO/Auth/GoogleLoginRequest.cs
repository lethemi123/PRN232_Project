namespace ProjectPRN232.DTO.Auth
{
    public class GoogleLoginRequest
    {
        public string Email { get; set; } = null!;
        public string? FName { get; set; }
        public string? LName { get; set; }
    }
}
