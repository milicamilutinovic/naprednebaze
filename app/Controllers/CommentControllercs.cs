using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using app.Models;
using Neo4j.Driver;
using System.Runtime.InteropServices;

namespace app.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CommentController : ControllerBase
    {
        private readonly IGraphClient _graphClient;

        public CommentController(IGraphClient graphClient)
        {
            _graphClient = graphClient;
        }

        // POST: /Comment
        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] Comment comment)
        {
            try
            {
                if (comment.Author == null || comment.Post == null)
                {
                    return BadRequest("Author and Post information are required.");
                }



                await _graphClient.Cypher
                                 .Match("(u:User {userId: $authorId})", "(p:Post {postId: $postId})")
                                 .WithParams(new
                                 {
                                     commentId = comment.CommentId,
                                     content = comment.Content,
                                     authorId = comment.Author.UserId,
                                     postId = comment.Post.postId,
                                     createdAt = comment.CreatedAt
                                 })
                                 .Create("(c:Comment {commentId: $commentId, content: $content, createdAt: $createdAt})") // promenjeno
                                 .Create("(u)-[:AUTHORED]->(c)")
                                 .Create("(c)-[:BELONGS_TO]->(p)")
                                 .ExecuteWithoutResultsAsync();


                return Ok(new { Message = "Comment created successfully", Comment = comment });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        //da se proveri ova get metoda, varaca mi 404, a u bazi posotij taj podatak
        //NE ZNAM STO NECE!!!!!!!!!!!!!!!!!!!!
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCommentById(string id)
        {
            try
            {
                // Logovanje vrednosti id
                Console.WriteLine($"Looking for Comment with ID: {id}");

                var query = await _graphClient.Cypher
                    // Match Comment sa njegovim vezama ka User i Post
                    .Match("(c:Comment {commentId: $commentId})-[:AUTHORED]->(u:User), (c)-[:BELONGS_TO]->(p:Post)")
                    .WithParam("commentId", id)
                    // Vraćanje podataka o Comment-u, Author-u i Post-u
                    .Return((c, u, p) => new
                    {
                        Comment = c.As<Comment>(),
                        Author = u.As<User>(),
                        Post = p.As<Post>()
                    })
                    .ResultsAsync;

                var result = query.FirstOrDefault();

                if (result == null)
                {
                    return NotFound(new { Message = "Comment not found" });
                }

                var commentData = result;

                var comment = commentData.Comment;
                comment.Author = commentData.Author;
                comment.Post = commentData.Post;

                return Ok(comment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }





        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(string id, [FromBody] Comment updatedComment)
        {
            if (string.IsNullOrEmpty(updatedComment.CommentId) || string.IsNullOrEmpty(updatedComment.Content))
            {
                return BadRequest("CommentId and Content are required.");
            }

            try
            {
                // Proverite da li je CommentId ispravan
                var query = await _graphClient.Cypher
                    .Match("(c:Comment {commentId: $commentId})")
                    .WithParam("commentId", id)
                    .Set("c.content = $content, c.createdAt = $createdAt")
                    .WithParams(new
                    {
                        content = updatedComment.Content,
                        createdAt = updatedComment.CreatedAt
                    })
                    .Return(c => c.As<Comment>())
                    .ResultsAsync;

                var result = query.FirstOrDefault();

                if (result == null)
                {
                    return NotFound(new { Message = "Comment not found" });
                }

                return Ok(new { Message = "Comment updated successfully", UpdatedComment = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }



        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(string id)
        {
            try
            {
                // Cypher query to delete the comment
                await _graphClient.Cypher
                    .Match("(c:Comment {commentId: $commentId})")
                    .WithParam("commentId", id)
                    .DetachDelete("c")
                    .ExecuteWithoutResultsAsync();

                // Return success message
                return Ok(new { Message = "Comment deleted successfully" });
            }
            catch (Exception ex)
            {
                // Handle errors
                return StatusCode(500, new { Error = ex.Message });
            }
        }

    }
}
