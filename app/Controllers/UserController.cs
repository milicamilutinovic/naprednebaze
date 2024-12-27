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
            try
            {
                // Generate a new userId if it is not provided
                if (string.IsNullOrEmpty(user.UserId))
                {
                    user.UserId = Guid.NewGuid().ToString(); // Generates a unique userId
                }

                var query = @"
            CREATE (u:User {
                userId: $userId,
                username: $username,
                fullName: $fullName,
                email: $email,
                passwordHash: $passwordHash,
                profilePicture: $profilePicture,
                bio: $bio,
                createdAt: $createdAt,
                isAdmin: $isAdmin
            })";

                var parameters = new
                {
                    userId = user.UserId,
                    username = user.Username,
                    fullName = user.FullName,
                    email = user.Email,
                    passwordHash = user.PasswordHash,
                    profilePicture = user.ProfilePicture,
                    bio = user.Bio,
                    createdAt = user.CreatedAt,
                    isAdmin = user.IsAdmin
                };

                // Execute the query to create the user in the Neo4j database
                await _graphClient.Cypher
                    .WithParams(parameters)
                    .Create(query)
                    .ExecuteWithoutResultsAsync();

                // Return the created user data as a response
                return Ok(new { Message = "User created successfully", User = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }


        // GET: api/User/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            try
            {
                var query = "MATCH (u:User {userId: $userId}) RETURN u";
                var result = await _graphClient.Cypher
                    .WithParams(new { userId = id })
                    .Match(query)
                    .Return(u => u.As<INode>())  // Return the node directly
                    .ResultsAsync;

                var user = result.SingleOrDefault();
                if (user == null)
                {
                    return NotFound(new { Message = "User not found" });
                }

                return Ok(user.Properties);
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
