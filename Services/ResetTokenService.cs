using Microsoft.AspNetCore.DataProtection;
using System.Globalization;

namespace ProjectPRN232.Services
{
    public class ResetTokenService
    {
        private readonly IDataProtector _protector;

        public ResetTokenService(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector("reset-password-v1");
        }

        public string CreateToken(string email, int minutes = 15)
        {
            var expiry = DateTime.UtcNow.AddMinutes(minutes).ToString("O");
            var payload = $"{email}|{expiry}";
            return _protector.Protect(payload);
        }

        public bool TryReadToken(string token, out string email)
        {
            email = "";
            try
            {
                var payload = _protector.Unprotect(token);
                var parts = payload.Split('|');
                if (parts.Length != 2) return false;

                email = parts[0];
                var expiry = DateTime.Parse(parts[1], null, DateTimeStyles.RoundtripKind);
                return expiry >= DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }
    }
}
