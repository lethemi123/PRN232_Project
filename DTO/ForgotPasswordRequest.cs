namespace ProjectPRN232.DTO
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = null!;
    }

    public class ForgotPasswordResponse
    {
        public string Message { get; set; } = null!;
        public string? DebugLink { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
