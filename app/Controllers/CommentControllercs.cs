using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using app.Models;
using Neo4j.Driver;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace app.Controllers
{
    [Route("[controller]")]
    [ApiController]

    public class CommentController : ControllerBase
    {
        private readonly IGraphClient _graphClient;

        public CommentController(IGraphClient graphClient)
        {
            _graphClient = graphClient;
        }

        // POST: /Comment
       [HttpPost]
        [HttpPost("AddComment")]

        public async Task<IActionResult> AddComment([FromBody] Comment comment)
        {
            try
            {
                Console.WriteLine($"Received Comment Data: {JsonSerializer.Serialize(comment)}");

                if (comment.Author == null || string.IsNullOrEmpty(comment.Author.UserId) ||
                    comment.Post == null || string.IsNullOrEmpty(comment.Post.postId))
                {
                    return BadRequest("Author UserId and PostId are required.");
                }

                // Proveri postojanje korisnika i posta
                var existsQuery = await _graphClient.Cypher
                    .Match("(u:User {userId: $authorId})", "(p:Post {postId: $postId})")
                    .WithParams(new
                    {
                        authorId = comment.Author.UserId,
                        postId = comment.Post.postId
                    })
                    .Return((u, p) => new
                    {
                        UserExists = u != null,
                        PostExists = p != null
                    })
                    .ResultsAsync;

                var existsResult = existsQuery.FirstOrDefault();

                if (existsResult == null || !existsResult.UserExists || !existsResult.PostExists)
                {
                    return NotFound(new { Message = "Author or Post not found." });
                }

                // Kreiraj komentar i poveži ga sa korisnikom i postom
                if (string.IsNullOrEmpty(comment.CommentId))
                {
                    comment.CommentId = Guid.NewGuid().ToString();  // Generišite CommentId ako nije prosleđen
                }
                var createdAt = DateTime.UtcNow;

                await _graphClient.Cypher
                    .Match("(u:User {userId: $authorId})", "(p:Post {postId: $postId})")
                    .WithParams(new
                    {
                        comment.CommentId,
                        content = comment.Content,
                        authorId = comment.Author.UserId,
                        postId = comment.Post.postId,
                        createdAt
                    })
                    .Create("(c:Comment {commentId: $CommentId, content: $content, createdAt: $createdAt})")
                    .Create("(u)-[:AUTHORED]->(c)")
                    .Create("(c)-[:BELONGS_TO]->(p)")
                    .ExecuteWithoutResultsAsync();

                return Ok(new
                {
                    Message = "Comment created successfully.",
                    Comment = new
                    {
                        CommentId = comment.CommentId,
                        Content = comment.Content,
                        CreatedAt = createdAt,
                        AuthorId = comment.Author.UserId,
                        PostId = comment.Post.postId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("GetComments/{postId}")]
        public async Task<IActionResult> GetComments(string postId)
        {
            try
            {
                if (string.IsNullOrEmpty(postId))
                {
                    Console.WriteLine("Invalid postId provided.");
                    return BadRequest(new { Message = "Invalid postId." });
                }

                // Izvrši upit sa ispravnim povratnim tipom
                var commentsQuery = await _graphClient.Cypher
                    .Match("(p:Post {postId: $postId})<-[:BELONGS_TO]-(c:Comment)<-[:AUTHORED]-(u:User)")
                    .WithParam("postId", postId)
                    .Return((c, u) => new
                    {
                        Comment = c.As<Comment>(),
                        Author = u.As<User>()
                    })
                    .ResultsAsync;

                // Logujte svaki rezultat da proverite šta je vraćeno
                foreach (var result in commentsQuery)
                {
                    Console.WriteLine($"CommentId: {result.Comment.CommentId}, Content: {result.Comment.Content}");
                    Console.WriteLine($"AuthorName: {result.Author.FullName}");
                }

                // Proveri da li ima komentara
                if (commentsQuery == null || !commentsQuery.Any())
                {
                    Console.WriteLine($"No comments found for postId: {postId}");
                    return NotFound(new { Message = "No comments found for the provided postId." });
                }

                // Mapiraj rezultate u listu sa selektovanim svojstvima
                var mappedComments = commentsQuery.Select(c => new
                {
                    CommentId = c.Comment.CommentId,
                    Content = c.Comment.Content,
                    CreatedAt = c.Comment.CreatedAt?.ToString("o") ?? "No Date",
                    AuthorName = c.Author.FullName
                }).ToList();

                // Ispis broja pronađenih komentara
                Console.WriteLine($"Fetched {mappedComments.Count} comments for postId {postId}.");
                return Ok(mappedComments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching comments for postId {postId}: {ex}");
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
