namespace BlackDigital.Mvc.Services
{
    public interface IPasswordHashService
    {
        string HashPassword(string password, string? saltPasswod = null);
        bool VerifyPassword(string password, string hashedPassword, string? saltPasswod = null);
    }
}
