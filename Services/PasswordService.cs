using Microsoft.AspNetCore.Identity;

namespace RaceDay1.Services
{
    //checks password verifications
    public class PasswordService
    {
        private readonly PasswordHasher<string> _hasher = new PasswordHasher<string>();
        public string HashPassword(string password)
        {
            return _hasher.HashPassword("RaceDayUser", password);
        }

        public bool VerifyPassword(string hashedPassword, string password)
        {
            var result = _hasher.VerifyHashedPassword("RaceDayUser", hashedPassword, password);
            return result != PasswordVerificationResult.Failed;
        }
    }
}
