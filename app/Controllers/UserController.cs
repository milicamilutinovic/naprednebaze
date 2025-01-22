using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System;
using System.Linq;
using System.Threading.Tasks;
using app.Models;
using Neo4j.Driver;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace app.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : Controller
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
        [HttpGet("/User/UserPage")]
        public async Task<IActionResult> UserPage()
        {
            try
            {
                // Dohvatanje korisničkog imena iz trenutne sesije (kako bi se prikazali podaci tog korisnika)
                var username = User.Identity.Name; // Pretpostavljamo da je username sačuvan u identitetu korisnika

                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Upit za pretragu korisnika na osnovu username-a
                var query = _graphClient.Cypher
                    .Match("(u:User {username: $username})")  // Tražimo korisnika sa određenim username
                    .WithParam("username", username)  // Prosleđujemo parametar sa username
                    .Return(u => u.As<User>())  // Vraćamo korisničke podatke
                    .ResultsAsync;

                // Izvršavamo upit
                var result = await query;
                if (!result.Any()) // Ako nema rezultata, ispisujemo grešku
                {
                    return NotFound("No user found with the given username.");
                }
                var user = result.FirstOrDefault();

                // Ako korisnik nije pronađen, vraćamo NotFound
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Vraćamo korisničke podatke na view
                return View(user);
            }
            catch (Exception ex)
            {
                // U slučaju greške, vraćamo grešku
                return StatusCode(500, new { Error = ex.Message });
            }
        }




        // DELETE: api/User/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                //string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Proverite da li korisnik postoji
                var userExists = await _graphClient.Cypher
                    .Match("(u:User {userId: $userId})")
                    .WithParam("userId", id)
                    .Return<int>("count(u)")  // Broj korisnika sa datim userId
                    .ResultsAsync;

                if (userExists.Single() == 0)
                {
                    return Json(new { success = false, error = "User not found" });
                }

                // Ako korisnik postoji, obrišite ga
                var query = _graphClient.Cypher
                    .Match("(u:User {userId: $userId})")
                    .WithParam("userId", id)
                    .Delete("u")  // Briše čvor korisnika
                    .ExecuteWithoutResultsAsync();

                await query;
                _ = HttpContext.SignOutAsync();
                HttpContext.Session.Clear();
                foreach (var cookie in Request.Cookies.Keys)
                {
                    Response.Cookies.Delete(cookie, new CookieOptions
                    {
                        Path = "/",
                        Domain = "https://localhost:7010", // Set to your app's domain
                    });
                }

                //return NoContent();  // Vraća 204 status kod kada je resurs uspešno obrisan
                // Return success response
                return Json(new { success = true });
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

                    // Create a unique file name for the uploaded image
                    var uniqueFileName = Guid.NewGuid() + Path.GetExtension(profilePicture.FileName);
                    var filePath = Path.Combine(uploadsDirectory, uniqueFileName);

                    // Save the file to the server
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePicture.CopyToAsync(stream);
                    }

                    profilePicturePath = "/images/" + uniqueFileName;
                }

                // Update user in the database
                await _graphClient.Cypher
                    .Match("(u:User {userId: $userId})")
                    .WithParam("userId", id)
                    .Set("u.bio = $Bio, u.profilePicture = $ProfilePicture")
                    .WithParam("Bio", bio)
                    .WithParam("ProfilePicture", profilePicturePath ?? string.Empty)
                    .ExecuteWithoutResultsAsync();

                // Fetch the updated user to ensure changes are reflected
                var updatedUser = await _graphClient.Cypher
                    .Match("(u:User {userId: $userId})")
                    .WithParam("userId", id)
                    .Return<User>("u")
                    .ResultsAsync;

                return Json(new { success = true, user = updatedUser.FirstOrDefault() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }



        // Action to handle adding a friend
        [HttpPost]
        public async Task<IActionResult> AddFriend(string username)
        {
            var currentUser = (await _graphClient.Cypher
                .Match("(u:User {username: $Username})")
                .WithParam("Username", User.Identity.Name)
                .Return(u => u.As<User>())
                .ResultsAsync)
                .FirstOrDefault();

            var friend = (await _graphClient.Cypher
                .Match("(u:User {username: $Username})")
                .WithParam("Username", username)
                .Return(u => u.As<User>())
                .ResultsAsync)
                .FirstOrDefault();

            if (friend != null && currentUser != null)
            {
                // Create a relationship between currentUser and friend
                await _graphClient.Cypher
                    .Match("(u:User {userId: $CurrentUserId}), (f:User {userId: $FriendUserId})")
                    .WithParams(new { CurrentUserId = currentUser.UserId, FriendUserId = friend.UserId })
                    .Merge("(u)-[:FRIEND]->(f)") // Create FRIEND relationship
                    .ExecuteWithoutResultsAsync();
            }
            return RedirectToAction("UserProfile");
        }
        // GET: /User/SearchUsernames
        [HttpGet("SearchUsernames")]
        public async Task<IActionResult> SearchUsernames(string query)
        {
            try
            {
                // Pretraga korisničkih imena prema upitu
                var queryResult = await _graphClient.Cypher
                    .Match("(u:User)")
                    .Where("u.username CONTAINS $query")
                    .WithParam("query", query)
                    .Return(u => u.As<User>())
                    .ResultsAsync;

                var usernames = queryResult.Select(user => user.Username).ToList();

                return Ok(usernames);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

    }
}
