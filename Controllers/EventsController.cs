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
        [HttpGet]
        public IActionResult GetEvents()
        {
            List<RaceDay1.Models.@event> events = new List<RaceDay1.Models.@event>();
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                SqlCommand command = new SqlCommand(
                    "SELECT * FROM Events", connection);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    events.Add(new RaceDay1.Models.@event
                    {
                        EventId = Convert.ToInt32(reader["EventID"]),
                        OrganiserId = Convert.ToInt32(reader["OrganiserID"]),
                        EventName = reader["EventName"].ToString() ?? "",
                        EventDate = Convert.ToDateTime(reader["EventDate"]),
                        Location = reader["Location"].ToString() ?? "",
                        Description = reader["Description"].ToString() ?? ""
                    });
                }

                reader.Close();
            }

            return Ok(events);
        }

        // GET: api/events/{eventId}
        [HttpGet("{eventId}")]
        public IActionResult GetEvent(int eventId)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                SqlCommand command = new SqlCommand(
                    "SELECT * FROM Events WHERE EventID = @EventID",
                    connection);

                command.Parameters.AddWithValue("@EventID", eventId);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                if (!reader.Read())
                {
                    reader.Close();
                    return NotFound("Event not found.");
                }

                var eventItem = new RaceDay1.Models.@event
                {
                    EventId = Convert.ToInt32(reader["EventID"]),
                    OrganiserId = Convert.ToInt32(reader["OrganiserID"]),
                    EventName = reader["EventName"].ToString() ?? "",
                    EventDate = Convert.ToDateTime(reader["EventDate"]),
                    Location = reader["Location"].ToString() ?? "",
                    Description = reader["Description"].ToString() ?? ""
                };

                reader.Close();
                return Ok(eventItem);
            }
        }

        // POST: api/events
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public IActionResult CreateEvent(RaceDay1.Models.@event eventItem)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = @"INSERT INTO Events
                    (OrganiserID, EventName, EventDate, Location, Description)
                    VALUES
                    (@OrganiserID, @EventName, @EventDate, @Location, @Description)";

                SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@OrganiserID", eventItem.OrganiserId);
                command.Parameters.AddWithValue("@EventName", eventItem.EventName);
                command.Parameters.AddWithValue("@EventDate", eventItem.EventDate);
                command.Parameters.AddWithValue("@Location", eventItem.Location);
                command.Parameters.AddWithValue("@Description",
                    (object?)eventItem.Description ?? DBNull.Value);

                connection.Open();
                command.ExecuteNonQuery();
            }

            return Ok("Event created successfully.");
        }

        // PUT: api/events/{eventId}
        [Authorize(Roles = "Organiser")]
        [HttpPut("{eventId}")]
        public IActionResult UpdateEvent(
            int eventId, RaceDay1.Models.@event eventItem)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = @"UPDATE Events
                    SET OrganiserID = @OrganiserID,
                        EventName = @EventName,
                        EventDate = @EventDate,
                        Location = @Location,
                        Description = @Description
                    WHERE EventID = @EventID";

                SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@EventID", eventId);
                command.Parameters.AddWithValue("@OrganiserID", eventItem.OrganiserId);
                command.Parameters.AddWithValue("@EventName", eventItem.EventName);
                command.Parameters.AddWithValue("@EventDate", eventItem.EventDate);
                command.Parameters.AddWithValue("@Location", eventItem.Location);
                command.Parameters.AddWithValue("@Description",
                    (object?)eventItem.Description ?? DBNull.Value);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound("Event not found.");
                }
            }

            return Ok("Event updated successfully.");
        }

        // DELETE: api/events/{eventId}
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{eventId}")]
        public IActionResult DeleteEvent(int eventId)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                SqlCommand command = new SqlCommand(
                    "DELETE FROM Events WHERE EventID = @EventID",
                    connection);

                command.Parameters.AddWithValue("@EventID", eventId);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound("Event not found.");
                }
            }

            return Ok("Event deleted successfully.");
        }
    }
}
