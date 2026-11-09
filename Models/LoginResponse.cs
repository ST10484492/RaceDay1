namespace RaceDay1.Models
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // PART 3: first name, shown in the web app navigation bar.
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
