using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay1.Models;
using System.Security.Claims;

namespace RaceDay1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResultsController : ControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        // GET: api/Results
        // Public endpoint for viewing results.
        [HttpGet]
        public async Task<IActionResult> GetResults()
        {
            var results = await _context.Results
                .AsNoTracking()
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/Results/mine
        // PART 3: the logged-in participant's race history, including
        // their finishing position and how many people finished that event.
        [Authorize(Roles = "Participant")]
        [HttpGet("mine")]
        public async Task<IActionResult> GetMyResults()
        {
            // Find this user's ParticipantID from the JWT (same approach as EnrolmentsController).
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized();

            await using var conn = new connection().GetConnection();
            await conn.OpenAsync();

            int participantId;
            using (var pc = new SqlCommand("SELECT ParticipantID FROM dbo.Participants WHERE UserId = @U", conn))
            {
                pc.Parameters.AddWithValue("@U", userId);
                object? o = await pc.ExecuteScalarAsync();
                if (o == null) return NotFound("Participant profile not found.");
                participantId = Convert.ToInt32(o);
            }

            // TotalFinishers = number of results with a position recorded for the same event.
            const string sql = @"
                SELECT ev.EventName, ev.EventDate, c.CategoryName,
                       r.FinishTime, r.Position,
                       (SELECT COUNT(*)
                        FROM dbo.Results r2
                        JOIN dbo.Enrolments e2 ON e2.EnrolmentID = r2.EnrolmentID
                        JOIN dbo.Categories c2 ON c2.CategoryID = e2.CategoryID
                        WHERE c2.EventID = ev.EventID AND r2.Position IS NOT NULL) AS TotalFinishers
                FROM dbo.Results r
                JOIN dbo.Enrolments e ON e.EnrolmentID = r.EnrolmentID
                JOIN dbo.Categories c ON c.CategoryID = e.CategoryID
                JOIN dbo.Events ev    ON ev.EventID = c.EventID
                WHERE e.ParticipantID = @P
                ORDER BY ev.EventDate DESC";

            var list = new List<object>();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@P", participantId);
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new
                {
                    EventName = rd.GetString(0),
                    EventDate = rd.GetDateTime(1),
                    CategoryName = rd.GetString(2),
                    // TimeSpan -> text such as "00:45:12" so the web app can show it directly
                    FinishTime = rd.IsDBNull(3) ? "" : rd.GetTimeSpan(3).ToString(@"hh\:mm\:ss"),
                    Position = rd.IsDBNull(4) ? 0 : rd.GetInt32(4),
                    TotalFinishers = rd.GetInt32(5)
                });
            }
            return Ok(list);
        }

        // One line of the organiser's results form.
        public class ResultEntry
        {
            public int EnrolmentId { get; set; }
            public string? FinishTime { get; set; }   // "hh:mm:ss"
            public int? Position { get; set; }
        }

        // POST: api/events/{eventId}/results
        // PART 3: organiser publishes finish times and positions for many participants at once.
        // Existing results are updated, new ones are inserted.
        [Authorize(Roles = "Organiser")]
        [HttpPost("~/api/events/{eventId:int}/results")]
        public async Task<IActionResult> PublishResults(int eventId, [FromBody] List<ResultEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return BadRequest("No results were supplied.");

            // Only accept enrolments that really belong to this event.
            var validIds = await (
                from en in _context.Enrolments
                join c in _context.Categories on en.CategoryId equals c.CategoryId
                where c.EventID == eventId
                select en.EnrolmentId).ToListAsync();

            foreach (var entry in entries)
            {
                if (!validIds.Contains(entry.EnrolmentId))
                    return BadRequest("One of the enrolments does not belong to this event.");

                if (!TimeSpan.TryParse(entry.FinishTime, out TimeSpan time))
                    return BadRequest("Finish times must look like 00:45:12.");

                var existing = await _context.Results
                    .FirstOrDefaultAsync(r => r.EnrolmentId == entry.EnrolmentId);

                if (existing == null)
                {
                    _context.Results.Add(new Result
                    {
                        EnrolmentId = entry.EnrolmentId,
                        FinishTime = time,
                        Position = entry.Position,
                        RecordedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.FinishTime = time;
                    existing.Position = entry.Position;
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Results published.");
        }

        // GET: api/Results/5
        // Return one result by its ID.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetResult(int id)
        {
            if (id <= 0)
                return BadRequest("A valid result ID is required.");

            var result = await _context.Results
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResultId == id);

            if (result == null)
                return NotFound("Result not found.");

            return Ok(result);
        }

        // POST: api/Results
        // Only organisers can create results.
        [Authorize(Roles = "Organiser")]
        [HttpPost]
        public async Task<IActionResult> CreateResult(Result result)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Do not allow an invalid enrolment reference.
            bool enrolmentExists = await _context.Enrolments
                .AnyAsync(e => e.EnrolmentId == result.EnrolmentId);

            if (!enrolmentExists)
                return BadRequest("The enrolment does not exist.");

            // SQL Server generates the new result ID.
            result.ResultId = 0;

            // Record when the result was added.
            result.RecordedAt = DateTime.UtcNow;

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetResult),
                new { id = result.ResultId },
                result);
        }

        // PUT: api/Results/5
        // Only organisers can update results.
        [Authorize(Roles = "Organiser")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateResult(
            int id, Result updatedResult)
        {
            if (id <= 0)
                return BadRequest("A valid result ID is required.");

            if (id != updatedResult.ResultId)
                return BadRequest("The result ID does not match.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _context.Results.FindAsync(id);

            if (result == null)
                return NotFound("Result not found.");

            bool enrolmentExists = await _context.Enrolments
                .AnyAsync(e => e.EnrolmentId == updatedResult.EnrolmentId);

            if (!enrolmentExists)
                return BadRequest("The enrolment does not exist.");

            // Update editable fields without changing the original record time.
            result.EnrolmentId = updatedResult.EnrolmentId;
            result.FinishTime = updatedResult.FinishTime;
            result.Position = updatedResult.Position;

            await _context.SaveChangesAsync();

            return Ok(result);
        }

        // DELETE: api/Results/5
        // Only organisers can delete results.
        [Authorize(Roles = "Organiser")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            if (id <= 0)
                return BadRequest("A valid result ID is required.");

            var result = await _context.Results.FindAsync(id);

            if (result == null)
                return NotFound("Result not found.");

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}