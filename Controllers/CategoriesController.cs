using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using RaceDay1.Models;
using RaceDay1;
using System;
using System.Collections.Generic;
using System.Data;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api")]
    public class CategoriesController : ControllerBase
    {
        // GET: api/events/{eventId}/categories
        [HttpGet("events/{eventId}/categories")]
        public IActionResult GetCategories(int eventId)
        {
            List<categories> categoriesList = new List<categories>();
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = "SELECT * FROM Categories WHERE EventID = @EventID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@EventID", eventId);

                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    categoriesList.Add(new categories
                    {
                        CategoryId = Convert.ToInt32(reader["CategoryID"]),
                        EventID = Convert.ToInt32(reader["EventID"]),
                        CategoryName = reader["CategoryName"].ToString() ?? "",
                        DistanceKM = Convert.ToDecimal(reader["DistanceKM"]),
                        EntryFee = Convert.ToDecimal(reader["EntryFee"])
                    });
                }

                reader.Close();
            }

            return Ok(categoriesList);
        }

        // POST: api/events/{eventId}/categories
        [Authorize(Roles = "Organiser")]
        [HttpPost("events/{eventId}/categories")]
        public IActionResult AddCategory(int eventId, categories category)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = @"INSERT INTO Categories
                    (EventID, CategoryName, DistanceKM, EntryFee)
                    VALUES
                    (@EventID, @CategoryName, @DistanceKM, @EntryFee)";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@EventID", eventId);
                command.Parameters.AddWithValue("@CategoryName", category.CategoryName);
                command.Parameters.AddWithValue("@DistanceKM", category.DistanceKM);
                command.Parameters.AddWithValue("@EntryFee", category.EntryFee);

                connection.Open();
                command.ExecuteNonQuery();
            }

            return Ok("Category added successfully.");
        }

        // PUT: api/categories/{categoryId}
        [Authorize(Roles = "Organiser")]
        [HttpPut("categories/{categoryId}")]
        public IActionResult UpdateCategory(int categoryId, categories category)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = @"UPDATE Categories
                    SET EventID = @EventID,
                        CategoryName = @CategoryName,
                        DistanceKM = @DistanceKM,
                        EntryFee = @EntryFee
                    WHERE CategoryID = @CategoryID";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CategoryID", categoryId);
                command.Parameters.AddWithValue("@EventID", category.EventID);
                command.Parameters.AddWithValue("@CategoryName", category.CategoryName);
                command.Parameters.AddWithValue("@DistanceKM", category.DistanceKM);
                command.Parameters.AddWithValue("@EntryFee", category.EntryFee);

                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound("Category not found.");
                }
            }

            return Ok("Category updated successfully.");
        }

        // DELETE: api/categories/{categoryId}
        [Authorize(Roles = "Organiser")]
        [HttpDelete("categories/{categoryId}")]
        public IActionResult DeleteCategory(int categoryId)
        {
            connection db = new connection();

            using (SqlConnection connection = db.GetConnection())
            {
                string query = "DELETE FROM Categories WHERE CategoryID = @CategoryID";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@CategoryID", categoryId);

                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound("Category not found.");
                }
            }

            return Ok("Category deleted successfully.");
        }
    }
}