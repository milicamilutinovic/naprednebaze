using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System;
using System.Threading.Tasks;
using app.Models;

namespace app.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LikeController : ControllerBase
    {
        private readonly IGraphClient _graphClient;

        public LikeController(IGraphClient graphClient)
        {
            _graphClient = graphClient;
        }

        // POST: /like
        [HttpPost]
        public async Task<IActionResult> LikePost([FromBody] Like like)
        {
            try
            {
                // Ensure that both User and Post are provided in the request
                if (like.user == null || like.post == null)
                {
                    return BadRequest("User or Post is missing.");
                }

                // Create a 'LIKES' relationship between the user and post
                //var query = @"
                //    MATCH (u:User {userId: $userId}), (p:Post {postId: $postId})
                //    CREATE (u)-[:LIKES]->(p)";

                var parameters = new
                {
                    userId = like.user.UserId,
                    postId = like.post.postId
                };

                // Execute the query without expecting results (no assignment needed)
                await _graphClient.Cypher
                                         .Match("(u:User {userId: $userId})", "(p:Post {postId: $postId})")
                                         .WithParams(new { userId = like.user.UserId, postId = like.post.postId })
                                         .Create("(u)-[:LIKES]->(p)")
                                         .ExecuteWithoutResultsAsync();


                return Ok(new { Message = "Post liked successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // DELETE: /like
        // DELETE: /like
        [HttpDelete]
        public async Task<IActionResult> UnlikePost([FromBody] Like like)
        {
            try
            {
                // Ensure that both User and Post are provided in the request
                if (like.user == null || like.post == null)
                {
                    return BadRequest("User or Post is missing.");
                }

                // Remove the 'LIKES' relationship between the user and post
                await _graphClient.Cypher
                    .Match("(u:User {userId: $userId})-[r:LIKES]->(p:Post {postId: $postId})")
                    .WithParams(new { userId = like.user.UserId, postId = like.post.postId })
                    .Delete("r")  // Delete the relationship
                    .ExecuteWithoutResultsAsync();

                return Ok(new { Message = "Post unliked successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

    }
}