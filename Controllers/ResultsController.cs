using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RaceDay1.Models;
using System;
using System.Threading.Tasks;

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