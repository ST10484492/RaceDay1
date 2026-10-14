using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using RaceDay1;
using RaceDay1.Models;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnrolmentsController : ControllerBase
    {
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
                        EnrolmentID = Convert.ToInt32(reader["EnrolmentID"]),
                        ParticipantID = Convert.ToInt32(reader["ParticipantID"]),
                        CategoryID = Convert.ToInt32(reader["CategoryID"]),
                        Enrolmentdate = Convert.ToDateTime(reader["EnrolmentDate"]),
                        PayementStatus = reader["PaymentStatus"].ToString() ?? "",
                        RaceNumber = Convert.ToInt32(reader["RaceNumber"])
                    });
                }

                reader.Close();
            }

            return Ok(enrolments);
        }
    }
}
