using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay1.Models;
using Microsoft.AspNetCore.Authorization;

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
        [HttpGet]
        public async Task<IActionResult> GetResults()
        {
            var results = await _context.Results
                .AsNoTracking()
                .ToListAsync();

            return Ok(results);
        }

        // GET: api/Results/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetResult(int id)
        {
            var result = await _context.Results.FindAsync(id);

            if (result == null)
                return NotFound("Result not found.");

            return Ok(result);
        }

        [Authorize(Roles = "Organiser")]
        // POST: api/Results
        [HttpPost]
        public async Task<IActionResult> CreateResult(Result result)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool enrolmentExists = await _context.Enrolments
                .AnyAsync(e => e.EnrolmentId == result.EnrolmentId);

            if (!enrolmentExists)
                return BadRequest("The enrolment does not exist.");

            result.ResultId = 0;
            result.RecordedAt = DateTime.UtcNow;

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetResult),
                new { id = result.ResultId },
                result);
        }

        [Authorize(Roles = "Organiser")]
        // PUT: api/Results/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateResult(
            int id, Result updatedResult)
        {
            if (id != updatedResult.ResultId)
                return BadRequest("The result ID does not match.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool enrolmentExists = await _context.Enrolments
                .AnyAsync(e => e.EnrolmentId == updatedResult.EnrolmentId);

            if (!enrolmentExists)
                return BadRequest("The enrolment does not exist.");

            var result = await _context.Results.FindAsync(id);

            if (result == null)
                return NotFound("Result not found.");

            result.EnrolmentId = updatedResult.EnrolmentId;
            result.FinishTime = updatedResult.FinishTime;
            result.Position = updatedResult.Position;

            await _context.SaveChangesAsync();

            return Ok(result);
        }

        [Authorize(Roles = "Organiser")]
        // DELETE: api/Results/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResult(int id)
        {
            var result = await _context.Results.FindAsync(id);

            if (result == null)
                return NotFound("Result not found.");

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
