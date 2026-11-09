using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RaceDay1;
using System.Security.Claims;

namespace RaceDay1.Controllers
{
    // PART 3: data for the Organiser dashboard page.
    // Route is "api/organiser" (singular) so it does not clash with OrganisersController.
    [ApiController]
    [Route("api/organiser")]
    [Authorize(Roles = "Organiser")]
    public class OrganiserDashboardController : ControllerBase
    {
        // GET: api/organiser/dashboard
        // Total events, total enrolments and a per-event summary for the logged-in organiser.
        [HttpGet("dashboard")]
        public IActionResult GetDashboard()
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);
            connection db = new connection();

            using SqlConnection conn = db.GetConnection();
            conn.Open();

            // Find the organiser that belongs to this login (matched by email).
            int organiserId;
            using (var orgCmd = new SqlCommand(
                "SELECT OrganiserID FROM dbo.Organisers WHERE ContactEmail = @Email", conn))
            {
                orgCmd.Parameters.AddWithValue("@Email", email ?? "");
                object? result = orgCmd.ExecuteScalar();
                if (result == null) return NotFound("No organiser record matches this account's email.");
                organiserId = Convert.ToInt32(result);
            }

            // One row per event with how many people enrolled.
            var events = new List<object>();
            int totalEnrolments = 0;

            using (var cmd = new SqlCommand(
                @"SELECT ev.EventName, ev.EventDate, COUNT(en.EnrolmentID) AS Enrolments
                  FROM dbo.Events ev
                  LEFT JOIN dbo.Categories c  ON c.EventID = ev.EventID
                  LEFT JOIN dbo.Enrolments en ON en.CategoryID = c.CategoryID
                  WHERE ev.OrganiserID = @Org
                  GROUP BY ev.EventID, ev.EventName, ev.EventDate
                  ORDER BY ev.EventDate", conn))
            {
                cmd.Parameters.AddWithValue("@Org", organiserId);
                using SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    int count = r.GetInt32(2);
                    totalEnrolments += count;
                    events.Add(new { Name = r.GetString(0), Date = r.GetDateTime(1), Enrolments = count });
                }
            }

            return Ok(new { TotalEvents = events.Count, TotalEnrolments = totalEnrolments, Events = events });
        }
    }
}
