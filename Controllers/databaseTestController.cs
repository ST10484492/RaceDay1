using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class databaseTestController : ControllerBase
    {
        [HttpGet]
        public IActionResult TestConnection()
        {

            connection db = new connection();

            if (db.TestConnection())
            {
                return Ok("Database connected successfully");
            }

            return StatusCode(500, "Database connection failed");
        }
    }
}
