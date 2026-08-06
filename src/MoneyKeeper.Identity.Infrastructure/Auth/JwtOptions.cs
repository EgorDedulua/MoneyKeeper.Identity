namespace MoneyKeeper.Identity.Infrastructure.Auth
{
    public class JwtOptions
    {
        public TimeSpan Expires { get; set; }

        public string SecretKey { get; set; } = string.Empty;

        public string Issuer { get; set; } = "MoneyKeeper.Identity";

        public string Audience { get; set; } = "MoneyKeeper";
    }
}
