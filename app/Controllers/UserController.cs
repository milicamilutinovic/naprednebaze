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
                    .Create("(u:User {userId: $UserId, username: $Username, fullName: $FullName, email: $Email, passwordHash: $PasswordHash, profilePicture: $ProfilePicture, bio: $Bio, createdAt: $CreatedAt, isAdmin: $IsAdmin})")
                    .WithParam("UserId", user.UserId)
                    .WithParam("Username", user.Username)
                    .WithParam("FullName", user.FullName)
                    .WithParam("Email", user.Email)
                    .WithParam("PasswordHash", user.PasswordHash)
                    .WithParam("ProfilePicture", user.ProfilePicture)
                    .WithParam("Bio", user.Bio)
                    .WithParam("CreatedAt", user.CreatedAt)
                    .WithParam("IsAdmin", user.IsAdmin)
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
                // Upit za traženje korisnika sa određenim UserId
                var query = _graphClient.Cypher
                    .Match("(u:User {userId: $userId})")  // Tražimo korisnika sa odgovarajućim UserId
                    .WithParam("userId", id)  // Prosleđujemo parametar sa UserId
                    .Return(u => u.As<User>())  // Vraćamo korisničke podatke
                    .ResultsAsync;

                // Izvršavamo upit
                var result = await query;
                var user = result.First();

                // Ako nije pronađen korisnik, vraćamo NotFound
                if (result == null || !result.Any())
                {
                    Console.WriteLine("No user found in the database");
                }
                else
                {
                    foreach (var u in result)
                    {
                        Console.WriteLine($"User found: {u.UserId} - {u.Username}");
                    }
                }

                // Vraćamo korisnika ako je pronađen
                return Ok(user);
            }
            catch (Exception ex)
            {
                // U slučaju greške, vraćamo 500 status sa greškom
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // DELETE: api/User/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                // Proverite da li korisnik postoji
                var userExists = await _graphClient.Cypher
                    .Match("(u:User {userId: $userId})")
                    .WithParam("userId", id)
                    .Return<int>("count(u)")  // Broj korisnika sa datim userId
                    .ResultsAsync;

                if (userExists.Single() == 0)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Ako korisnik postoji, obrišite ga
                var query = _graphClient.Cypher
                    .Match("(u:User {userId: $userId})")
                    .WithParam("userId", id)
                    .Delete("u")  // Briše čvor korisnika
                    .ExecuteWithoutResultsAsync();

                await query;

                return NoContent();  // Vraća 204 status kod kada je resurs uspešno obrisan
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
            if (user == null)
            {
                return BadRequest("User data is required.");
            }

            try
            {
                // Prvo proverite da li korisnik postoji u bazi
                var userExists = await _graphClient.Cypher
                    .Match("(u:User {userId: $userId})")
                    .WithParam("userId", id)
                    .Return<int>("count(u)")  // Broj korisnika sa datim userId
                    .ResultsAsync;

                if (userExists.Single() == 0)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Ažuriranje podataka korisnika
                var query = _graphClient.Cypher
                    .Match("(u:User {userId: $userId})")
                    .WithParam("userId", id)
                    .Set("u.username = $Username, u.fullName = $FullName, u.email = $Email, u.passwordHash = $PasswordHash, u.profilePicture = $ProfilePicture, u.bio = $Bio, u.isAdmin = $IsAdmin")
                    .WithParam("Username", user.Username)
                    .WithParam("FullName", user.FullName)
                    .WithParam("Email", user.Email)
                    .WithParam("PasswordHash", user.PasswordHash)
                    .WithParam("ProfilePicture", user.ProfilePicture)
                    .WithParam("Bio", user.Bio)
                    .WithParam("IsAdmin", user.IsAdmin)
                    .Return<string>("u.userId")  // Vraća userId korisnika
                    .ResultsAsync;

                var updatedUser = query.Result.FirstOrDefault();

                if (updatedUser == null)
                {
                    return StatusCode(500, new { message = "User update failed" });
                }

                return Ok(new { message = "User updated successfully", userId = updatedUser });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User user)
        {
            var query = _graphClient.Cypher
                .Match("(u:User {email: $Email, passwordHash: $PasswordHash})")
                .WithParam("Email", user.Email)
                .WithParam("PasswordHash", user.PasswordHash)
                .Return(u => u.As<User>())
                .ResultsAsync;

            var result = await query;

            if (!result.Any())
            {
                return Unauthorized("Invalid email or password.");
            }

            return Ok(result.First());
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            var query = _graphClient.Cypher
                .Create("(u:User {email: $Email, passwordHash: $PasswordHash, fullName: $FullName, profilePicture: $ProfilePicture, bio: $Bio})")
                .WithParam("Email", user.Email)
                .WithParam("PasswordHash", user.PasswordHash)
                .WithParam("FullName", user.FullName)
                .WithParam("ProfilePicture", user.ProfilePicture)
                .WithParam("Bio", user.Bio)
                .Return(u => u.As<User>())
                .ResultsAsync;

            var result = await query;

            if (!result.Any())
            {
                return StatusCode(500, "Error while registering.");
            }

            return CreatedAtAction(nameof(Register), new { id = user.UserId }, user);
        }


    }
}
