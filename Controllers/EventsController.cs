using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using RaceDay1.Models;
using RaceDay1;
using RaceDay1.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        // PART 3: Azure Blob helper, injected by ASP.NET Core (see Program.cs).
        private readonly BlobStorageService _blobs;

        public EventsController(BlobStorageService blobs)
        {
            _blobs = blobs;
        }

        // Columns returned for every event (kept in one place so queries stay consistent).
        private const string EventColumns =
            @"EventID, OrganiserID, EventName, EventDate, Location,
              Description, EventType, DistanceKm, BannerUrl";

        // Turns the current row of a reader into an event object.
        private static RaceDay1.Models.@event ReadEvent(SqlDataReader reader)
        {
            return new RaceDay1.Models.@event
            {
                EventId = Convert.ToInt32(reader["EventID"]),
                OrganiserId = Convert.ToInt32(reader["OrganiserID"]),
                EventName = reader["EventName"].ToString() ?? "",
                EventDate = Convert.ToDateTime(reader["EventDate"]),
                Location = reader["Location"].ToString() ?? "",
                Description = reader["Description"] == DBNull.Value
                    ? "" : reader["Description"].ToString() ?? "",
                EventType = reader["EventType"] == DBNull.Value
                    ? "Run" : reader["EventType"].ToString() ?? "Run",
                DistanceKm = reader["DistanceKm"] == DBNull.Value
                    ? 0 : Convert.ToDecimal(reader["DistanceKm"]),
                BannerUrl = reader["BannerUrl"] == DBNull.Value
                    ? null : reader["BannerUrl"].ToString()
            };
        }

        // PART 3: the Users table has no link to Organisers, so we match the
        // logged-in user's email to Organisers.ContactEmail.
        // Returns null if this user has no organiser record.
        private int? GetOrganiserId(SqlConnection conn)
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return null;

            using var cmd = new SqlCommand(
                "SELECT OrganiserID FROM dbo.Organisers WHERE ContactEmail = @Email", conn);
            cmd.Parameters.AddWithValue("@Email", email);

            object? result = cmd.ExecuteScalar();
            return result == null || result == DBNull.Value ? null : Convert.ToInt32(result);
        }

        // GET: api/events
        // Public endpoint: anyone can view the available events.
        [HttpGet]
        public IActionResult GetEvents()
        {
            List<RaceDay1.Models.@event> events = new();
            connection db = new connection();

            using SqlConnection conn = db.GetConnection();
            using SqlCommand command = new SqlCommand(
                $"SELECT {EventColumns} FROM dbo.Events ORDER BY EventDate", conn);

            conn.Open();
            using SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
                events.Add(ReadEvent(reader));

            return Ok(events);
        }

        // GET: api/events/mine
        // PART 3: only the events created by the logged-in Organiser.
        [Authorize(Roles = "Organiser")]
        [HttpGet("mine")]
        public IActionResult GetMyEvents()
        {
            List<RaceDay1.Models.@event> events = new();
            connection db = new connection();

            using SqlConnection conn = db.GetConnection();
            conn.Open();

            int? organiserId = GetOrganiserId(conn);
            if (organiserId == null)
                return NotFound("No organiser record matches this account's email.");

            using SqlCommand command = new SqlCommand(
                $"SELECT {EventColumns} FROM dbo.Events WHERE OrganiserID = @Org ORDER BY EventDate", conn);
            command.Parameters.AddWithValue("@Org", organiserId.Value);

            using SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
                events.Add(ReadEvent(reader));

            return Ok(events);
        }

        // GET: api/events/{eventId}
        // Public endpoint: returns one event or 404 if it doesn't exist.
        [HttpGet("{eventId:int}")]
        public IActionResult GetEvent(int eventId)
        {
            if (eventId <= 0)
                return BadRequest("A valid event ID is required.");

            connection db = new connection();

            using SqlConnection conn = db.GetConnection();
            using SqlCommand command = new SqlCommand(
                $"SELECT {EventColumns} FROM dbo.Events WHERE EventID = @EventID", conn);

            command.Parameters.AddWithValue("@EventID", eventId);

            conn.Open();
            using SqlDataReader reader = command.ExecuteReader();

            if (!reader.Read())
                return NotFound("Event not found.");

            return Ok(ReadEvent(reader));
        }

        // POST: api/events
        // Only authenticated Organisers can create events.
        // PART 3: OrganiserId is now optional in the body; if it is missing
        // we work it out from the logged-in user.
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public IActionResult CreateEvent(
            [FromBody] RaceDay1.Models.@event eventItem)
        {
            // Validate required details before accessing the database.
            if (eventItem == null ||
                string.IsNullOrWhiteSpace(eventItem.EventName) ||
                string.IsNullOrWhiteSpace(eventItem.Location) ||
                eventItem.EventDate == default)
            {
                return BadRequest(
                    "EventName, EventDate and Location are required.");
            }

            connection db = new connection();

            using SqlConnection conn = db.GetConnection();

            try
            {
                conn.Open();

                // Use the organiser from the token unless one was supplied.
                if (eventItem.OrganiserId <= 0)
                {
                    int? organiserId = GetOrganiserId(conn);
                    if (organiserId == null)
                        return BadRequest("No organiser record matches this account's email.");
                    eventItem.OrganiserId = organiserId.Value;
                }

                using SqlCommand command = new SqlCommand(
                    @"INSERT INTO dbo.Events
                        (OrganiserID, EventName, EventDate, Location,
                         Description, EventType, DistanceKm)
                      OUTPUT INSERTED.EventID
                      VALUES
                        (@OrganiserID, @EventName, @EventDate, @Location,
                         @Description, @EventType, @DistanceKm)", conn);

                command.Parameters.AddWithValue("@OrganiserID", eventItem.OrganiserId);
                command.Parameters.AddWithValue("@EventName", eventItem.EventName.Trim());
                command.Parameters.AddWithValue("@EventDate", eventItem.EventDate);
                command.Parameters.AddWithValue("@Location", eventItem.Location.Trim());
                command.Parameters.AddWithValue("@Description", (object?)eventItem.Description ?? DBNull.Value);
                command.Parameters.AddWithValue("@EventType", string.IsNullOrWhiteSpace(eventItem.EventType) ? "Run" : eventItem.EventType.Trim());
                command.Parameters.AddWithValue("@DistanceKm", eventItem.DistanceKm);

                // Return the new ID so the client can retrieve the event.
                int newEventId = Convert.ToInt32(command.ExecuteScalar());
                eventItem.EventId = newEventId;

                return CreatedAtAction(nameof(GetEvent), new { eventId = newEventId }, eventItem);
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                // Usually means the supplied OrganiserID violates a foreign key.
                return BadRequest("The selected organiser does not exist.");
            }
        }

        // PUT: api/events/{eventId}
        // Only Organisers can update events.
        [Authorize(Roles = "Organiser")]
        [HttpPut("{eventId:int}")]
        public IActionResult UpdateEvent(
            int eventId,
            [FromBody] RaceDay1.Models.@event eventItem)
        {
            if (eventId <= 0)
                return BadRequest("A valid event ID is required.");

            if (eventItem == null ||
                string.IsNullOrWhiteSpace(eventItem.EventName) ||
                string.IsNullOrWhiteSpace(eventItem.Location) ||
                eventItem.EventDate == default)
            {
                return BadRequest("EventName, EventDate and Location are required.");
            }

            connection db = new connection();

            using SqlConnection conn = db.GetConnection();
            using SqlCommand command = new SqlCommand(
                @"UPDATE dbo.Events
                  SET EventName = @EventName,
                      EventDate = @EventDate,
                      Location = @Location,
                      Description = @Description,
                      EventType = @EventType,
                      DistanceKm = @DistanceKm
                  WHERE EventID = @EventID", conn);

            // Keep the existing OrganiserID and BannerUrl unchanged during an update.
            command.Parameters.AddWithValue("@EventID", eventId);
            command.Parameters.AddWithValue("@EventName", eventItem.EventName.Trim());
            command.Parameters.AddWithValue("@EventDate", eventItem.EventDate);
            command.Parameters.AddWithValue("@Location", eventItem.Location.Trim());
            command.Parameters.AddWithValue("@Description", (object?)eventItem.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("@EventType", string.IsNullOrWhiteSpace(eventItem.EventType) ? "Run" : eventItem.EventType.Trim());
            command.Parameters.AddWithValue("@DistanceKm", eventItem.DistanceKm);

            conn.Open();

            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
                return NotFound("Event not found.");

            return Ok("Event updated successfully.");
        }

        // POST: api/events/{eventId}/banner
        // PART 3: Organiser uploads a banner image (multipart/form-data, field name "banner").
        // The file is stored in Azure Blob Storage; only its URL is saved in SQL.
        [Authorize(Roles = "Organiser")]
        [HttpPost("{eventId:int}/banner")]
        public async Task<IActionResult> UploadBanner(int eventId, [FromForm] IFormFile banner)
        {
            if (!BlobStorageService.IsValidImage(banner))
                return BadRequest("Please upload an image smaller than 5MB.");

            string url = await _blobs.UploadImageAsync(banner, "banners");

            connection db = new connection();
            await using SqlConnection conn = db.GetConnection();
            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand(
                "UPDATE dbo.Events SET BannerUrl = @Url WHERE EventID = @EventID", conn);
            cmd.Parameters.AddWithValue("@Url", url);
            cmd.Parameters.AddWithValue("@EventID", eventId);

            if (await cmd.ExecuteNonQueryAsync() == 0)
                return NotFound("Event not found.");

            return Ok(new { bannerUrl = url });
        }

        // GET: api/events/{eventId}/enrolments
        // PART 3: Organiser sees every participant enrolled in one event.
        [Authorize(Roles = "Organiser")]
        [HttpGet("{eventId:int}/enrolments")]
        public IActionResult GetEventEnrolments(int eventId)
        {
            var list = new List<object>();
            connection db = new connection();

            using SqlConnection conn = db.GetConnection();
            using SqlCommand cmd = new SqlCommand(
                @"SELECT e.EnrolmentID, e.PaymentStatus, e.RaceNumber,
                         p.FirstName, p.LastName, c.CategoryName
                  FROM dbo.Enrolments e
                  JOIN dbo.Participants p ON p.ParticipantID = e.ParticipantID
                  JOIN dbo.Categories c   ON c.CategoryID = e.CategoryID
                  WHERE c.EventID = @EventID
                  ORDER BY p.LastName, p.FirstName", conn);
            cmd.Parameters.AddWithValue("@EventID", eventId);

            conn.Open();
            using SqlDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new
                {
                    EnrolmentId = r.GetInt32(0),
                    PaymentStatus = r.GetString(1),
                    RaceNumber = r.IsDBNull(2) ? (int?)null : r.GetInt32(2),
                    ParticipantName = r.GetString(3) + " " + r.GetString(4),
                    CategoryName = r.GetString(5)
                });
            }
            return Ok(list);
        }

        // DELETE: api/events/{eventId}
        // Only Organisers can delete events.
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{eventId:int}")]
        public IActionResult DeleteEvent(int eventId)
        {
            if (eventId <= 0)
                return BadRequest("A valid event ID is required.");

            connection db = new connection();

            using SqlConnection conn = db.GetConnection();
            using SqlCommand command = new SqlCommand(
                "DELETE FROM dbo.Events WHERE EventID = @EventID", conn);

            command.Parameters.AddWithValue("@EventID", eventId);

            try
            {
                conn.Open();

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                    return NotFound("Event not found.");

                return Ok("Event deleted successfully.");
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                // The event may still have Categories referencing it.
                return Conflict("This event cannot be deleted because related records exist.");
            }
        }
    }
}
