using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using RaceDay1;
using RaceDay1.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/enrolments")]
    public class EnrolmentsController : ControllerBase
    {
        // GET: api/enrolments
        [Authorize(Roles = "Participant")]
        [HttpGet]
        public IActionResult GetEnrolments()
        {
            List<enrolment> enrolments = new List<enrolment>();
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                SqlCommand command = new SqlCommand(
                    "SELECT * FROM Enrolments", connection);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    enrolments.Add(new enrolment
                    {
                        EnrolmentId = Convert.ToInt32(reader["EnrolmentID"]),
                        ParticipantId = Convert.ToInt32(reader["ParticipantID"]),
                        CategoryId = Convert.ToInt32(reader["CategoryID"]),
                        Enrolmentdate = Convert.ToDateTime(reader["EnrolmentDate"]),
                        PayementStatus = reader["PaymentStatus"].ToString() ?? "",
                        RaceNumber = Convert.ToInt32(reader["RaceNumber"])
                    });
                }

                reader.Close();
            }

            return Ok(enrolments);
        }

        // GET: api/enrolments/{enrolmentId}
        [Authorize(Roles = "Participant")]
        [HttpGet("{enrolmentId}")]
        public IActionResult GetEnrolment(int enrolmentId)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                SqlCommand command = new SqlCommand(
                    "SELECT * FROM Enrolments WHERE EnrolmentID = @EnrolmentID",
                    connection);

                command.Parameters.AddWithValue("@EnrolmentID", enrolmentId);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                if (!reader.Read())
                {
                    reader.Close();
                    return NotFound("Enrolment not found.");
                }

                var item = new enrolment
                {
                    EnrolmentId = Convert.ToInt32(reader["EnrolmentID"]),
                    ParticipantId = Convert.ToInt32(reader["ParticipantID"]),
                    CategoryId = Convert.ToInt32(reader["CategoryID"]),
                    Enrolmentdate = Convert.ToDateTime(reader["EnrolmentDate"]),
                    PayementStatus = reader["PaymentStatus"].ToString() ?? "",
                    RaceNumber = Convert.ToInt32(reader["RaceNumber"])
                };

                reader.Close();
                return Ok(item);
            }
        }

        // POST: api/enrolments
        [Authorize(Roles = "Participant")]
        [HttpPost]
        public IActionResult CreateEnrolment(enrolment item)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = @"INSERT INTO Enrolments
                    (ParticipantID, CategoryID, EnrolmentDate, PaymentStatus, RaceNumber)
                    VALUES
                    (@ParticipantID, @CategoryID, @EnrolmentDate, @PaymentStatus, @RaceNumber)";

                SqlCommand command = new SqlCommand(query, connection);

                command.Parameters.AddWithValue("@ParticipantID", item.ParticipantId);
                command.Parameters.AddWithValue("@CategoryID", item.CategoryId);
                command.Parameters.AddWithValue("@EnrolmentDate", item.Enrolmentdate);
                command.Parameters.AddWithValue("@PaymentStatus", item.PayementStatus);
                command.Parameters.AddWithValue("@RaceNumber", item.RaceNumber);

                connection.Open();
                command.ExecuteNonQuery();
            }

            return Ok("Enrolment created successfully.");
        }

        [Authorize(Roles = "Participant")]
        [HttpPut("{enrolmentId}")]
        public IActionResult UpdateEnrolment(int enrolmentId, enrolment item)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                SqlCommand command = new SqlCommand(
                    @"UPDATE Enrolments
              SET ParticipantID = @ParticipantID,
                  CategoryID = @CategoryID,
                  EnrolmentDate = @EnrolmentDate,
                  PaymentStatus = @PaymentStatus,
                  RaceNumber = @RaceNumber
              WHERE EnrolmentID = @EnrolmentID",
                    connection);

                command.Parameters.AddWithValue("@EnrolmentID", enrolmentId);
                command.Parameters.AddWithValue("@ParticipantID", item.ParticipantId);
                command.Parameters.AddWithValue("@CategoryID", item.CategoryId);
                command.Parameters.AddWithValue("@EnrolmentDate", item.Enrolmentdate);
                command.Parameters.AddWithValue("@PaymentStatus", item.PayementStatus);
                command.Parameters.AddWithValue("@RaceNumber", item.RaceNumber);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound("Enrolment not found.");
                }
            }

            return Ok("Enrolment updated successfully.");
        }

        // DELETE: api/enrolments/{enrolmentId}
        [Authorize(Roles = "Participant")]
        [HttpDelete("{enrolmentId}")]
        public IActionResult DeleteEnrolment(int enrolmentId)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                SqlCommand command = new SqlCommand(
                    "DELETE FROM Enrolments WHERE EnrolmentID = @EnrolmentID",
                    connection);

                command.Parameters.AddWithValue("@EnrolmentID", enrolmentId);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound("Enrolment not found.");
                }
            }

            return Ok("Enrolment deleted successfully.");
        }
    }
}