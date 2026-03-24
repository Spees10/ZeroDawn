namespace ZeroDawn.Web.Configuration;

public class JwtOptions
{
    public const string Section = "Jwt";
    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int AccessTokenExpirationMinutes { get; set; } = 30;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
