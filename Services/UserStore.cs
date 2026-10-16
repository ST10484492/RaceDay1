using System.Collections.Generic;
using RaceDay1.Models;

namespace RaceDay1.Services
{
    public class UserStore
    {
        public List<User> Users { get; set; } = new List<User>();

        public UserStore(PasswordService passwordService)
        {
            Users.Add(new User
            {
                UserId = 1,
                FirstName = "Test",
                Email = "organiser@raceday.com",
                PasswordHash = passwordService.HashPassword("Organiser123!"),
                Role = "Organiser"
            });
        }
    }
}
