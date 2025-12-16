namespace ProjectPRN232.DTO.Auth
{
    public class RegisterRequest
    {
        public string UserName { get; set; } = null;
        public string Password { get; set; } = null;
        public string Email { get; set; } = null; 
        public string? FName { get; set; }
        public string? LName { get; set; }
    }
}
