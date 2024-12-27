using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System;
using System.Linq;
using System.Threading.Tasks;
using app.Models;
using Neo4j.Driver;

namespace app.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IGraphClient _graphClient;

        public UserController(IGraphClient graphClient)
        {
            _graphClient = graphClient;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            if (user == null)
            {
                return BadRequest("User data is required.");
            }

            // You can add more validation if needed
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Email))
            {
                return BadRequest("Username and Email are required.");
            }

            try
            {
                // Generate a unique UserId
                user.UserId = Guid.NewGuid().ToString();

                // Create a new user node in the database
                var query = _graphClient.Cypher
                    .Create("(u:User {userId: $userId, username: $username, fullName: $fullName, email: $email, passwordHash: $passwordHash, profilePicture: $profilePicture, bio: $bio, createdAt: $createdAt, isAdmin: $isAdmin})")
                    .WithParam("userId", user.UserId)
                    .WithParam("username", user.Username)
                    .WithParam("fullName", user.FullName)
                    .WithParam("email", user.Email)
                    .WithParam("passwordHash", user.PasswordHash)
                    .WithParam("profilePicture", user.ProfilePicture)
                    .WithParam("bio", user.Bio)
                    .WithParam("createdAt", user.CreatedAt)
                    .WithParam("isAdmin", user.IsAdmin)
                    .Return<string>("u.UserId");

                await query.ExecuteWithoutResultsAsync();

                // Return a response with the created UserId
                return CreatedAtAction(nameof(CreateUser), new { id = user.UserId }, user);
            }
            catch (Exception ex)
            {
                // Handle any exceptions (like connection issues, etc.)
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    


    // GET: api/User/{id}
    [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                    var result = await _graphClient.Cypher
         .Match("(u:User {userId: $userId})") // Ispravno MATCH
         .WithParams(new { userId = id })
         .Return(u => u.As<User>())  // Return the User object directly
         .ResultsAsync;

                    if (result == null || !result.Any())
                    {
                        return NotFound(new { message = "User not found" });
                    }

                    // Vraćanje korisnika
                    var user = result.First();
                    return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // DELETE: api/User/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var query = "MATCH (u:User {userId: $userId}) DETACH DELETE u";
                await _graphClient.Cypher
                    .WithParams(new { userId = id })
                    .Match(query)
                    .ExecuteWithoutResultsAsync();

                return Ok(new { Message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // PUT: api/User/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] User user)
        {
            try
            {
                var query = @"
                    MATCH (u:User {userId: $userId})
                    SET u.username = $username,
                        u.fullName = $fullName,
                        u.email = $email,
                        u.passwordHash = $passwordHash,
                        u.profilePicture = $profilePicture,
                        u.bio = $bio,
                        u.isAdmin = $isAdmin
                    RETURN u";

                var parameters = new
                {
                    userId = id,
                    username = user.Username,
                    fullName = user.FullName,
                    email = user.Email,
                    passwordHash = user.PasswordHash,
                    profilePicture = user.ProfilePicture,
                    bio = user.Bio,
                    isAdmin = user.isAdmin()
                };

                var result = await _graphClient.Cypher
                    .WithParams(parameters)
                    .Match(query)
                    .Return(u => u.As<INode>())
                    .ResultsAsync;

                var updatedUser = result.SingleOrDefault();
                if (updatedUser == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                return Ok(updatedUser.Properties);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
