using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using RaceDay1.Models;
using RaceDay1;
using System;
using System.Collections.Generic;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        // GET: api/events
        // Public endpoint: anyone can view the available events.
        [HttpGet]
        public IActionResult GetEvents()
        {
            List<RaceDay1.Models.@event> events = new();
            connection db = new connection();

            using SqlConnection conn = db.GetConnection();
            using SqlCommand command = new SqlCommand(
                @"SELECT EventID, OrganiserID, EventName,
                         EventDate, Location, Description
                  FROM dbo.Events", conn);

            conn.Open();

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                events.Add(new RaceDay1.Models.@event
                {
                    EventId = Convert.ToInt32(reader["EventID"]),
                    OrganiserId = Convert.ToInt32(reader["OrganiserID"]),
                    EventName = reader["EventName"].ToString() ?? "",
                    EventDate = Convert.ToDateTime(reader["EventDate"]),
                    Location = reader["Location"].ToString() ?? "",
                    Description = reader["Description"] == DBNull.Value
                        ? ""
                        : reader["Description"].ToString() ?? ""
                });
            }

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
                @"SELECT EventID, OrganiserID, EventName,
                         EventDate, Location, Description
                  FROM dbo.Events
                  WHERE EventID = @EventID", conn);

            command.Parameters.AddWithValue("@EventID", eventId);

            conn.Open();

            using SqlDataReader reader = command.ExecuteReader();

            if (!reader.Read())
                return NotFound("Event not found.");

            var eventItem = new RaceDay1.Models.@event
            {
                EventId = Convert.ToInt32(reader["EventID"]),
                OrganiserId = Convert.ToInt32(reader["OrganiserID"]),
                EventName = reader["EventName"].ToString() ?? "",
                EventDate = Convert.ToDateTime(reader["EventDate"]),
                Location = reader["Location"].ToString() ?? "",
                Description = reader["Description"] == DBNull.Value
                    ? ""
                    : reader["Description"].ToString() ?? ""
            };

            return Ok(eventItem);
        }

        // POST: api/events
        // Only authenticated Organisers can create events.
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public IActionResult CreateEvent(
            [FromBody] RaceDay1.Models.@event eventItem)
        {
            // Validate required details before accessing the database.
            if (eventItem == null ||
                eventItem.OrganiserId <= 0 ||
                string.IsNullOrWhiteSpace(eventItem.EventName) ||
                string.IsNullOrWhiteSpace(eventItem.Location) ||
                eventItem.EventDate == default)
            {
                return BadRequest(
                    "OrganiserId, EventName, EventDate and Location are required.");
            }

            connection db = new connection();

            using SqlConnection conn = db.GetConnection();
            using SqlCommand command = new SqlCommand(
                @"INSERT INTO dbo.Events
                    (OrganiserID, EventName, EventDate, Location, Description)
                  OUTPUT INSERTED.EventID
                  VALUES
                    (@OrganiserID, @EventName, @EventDate,
                     @Location, @Description)", conn);

            command.Parameters.AddWithValue(
                "@OrganiserID", eventItem.OrganiserId);
            command.Parameters.AddWithValue(
                "@EventName", eventItem.EventName.Trim());
            command.Parameters.AddWithValue(
                "@EventDate", eventItem.EventDate);
            command.Parameters.AddWithValue(
                "@Location", eventItem.Location.Trim());
            command.Parameters.AddWithValue(
                "@Description",
                (object?)eventItem.Description ?? DBNull.Value);

            try
            {
                conn.Open();

                // Return the new ID so the client can retrieve the event.
                int newEventId = Convert.ToInt32(command.ExecuteScalar());

                eventItem.EventId = newEventId;

                return CreatedAtAction(
                    nameof(GetEvent),
                    new { eventId = newEventId },
                    eventItem);
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                // Usually means the supplied OrganiserID violates a foreign key.
                return BadRequest(
                    "The selected organiser does not exist.");
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
                return BadRequest(
                    "EventName, EventDate and Location are required.");
            }

            connection db = new connection();

            using SqlConnection conn = db.GetConnection();
            using SqlCommand command = new SqlCommand(
                @"UPDATE dbo.Events
                  SET EventName = @EventName,
                      EventDate = @EventDate,
                      Location = @Location,
                      Description = @Description
                  WHERE EventID = @EventID", conn);

            // Keep the existing OrganiserID unchanged during an update.
            command.Parameters.AddWithValue("@EventID", eventId);
            command.Parameters.AddWithValue(
                "@EventName", eventItem.EventName.Trim());
            command.Parameters.AddWithValue(
                "@EventDate", eventItem.EventDate);
            command.Parameters.AddWithValue(
                "@Location", eventItem.Location.Trim());
            command.Parameters.AddWithValue(
                "@Description",
                (object?)eventItem.Description ?? DBNull.Value);

            conn.Open();

            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
                return NotFound("Event not found.");

            return Ok("Event updated successfully.");
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
                "DELETE FROM dbo.Events WHERE EventID = @EventID",
                conn);

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
                return Conflict(
                    "This event cannot be deleted because related records exist.");
            }
        }
    }
}
