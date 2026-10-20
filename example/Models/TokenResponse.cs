namespace BlackDigital.AspNet.Example.Models
{
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public string TokenType { get; set; } = "Bearer";

        public int ExpiresInSeconds { get; set; }
    }
}
