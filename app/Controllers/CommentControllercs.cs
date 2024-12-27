using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using app.Models;

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
                if (comment.author == null || comment.post == null)
                {
                    return BadRequest("Author and Post information are required.");
                }

                var query = @"
                    MATCH (u:User {userId: $authorId}), (p:Post {postId: $postId})
                    CREATE (c:Comment {commentId: $commentId, content: $content, createdAt: $createdAt})
                    CREATE (u)-[:AUTHORED]->(c)
                    CREATE (c)-[:BELONGS_TO]->(p)";

                var parameters = new
                {
                    commentId = comment.commentId,
                    content = comment.content,
                    createdAt = comment.createdAt.ToString(),
                    authorId = comment.author.UserId,
                    postId = comment.post.postId
                };

                await _graphClient.Cypher
                    .WithParams(parameters)
                    .ExecuteWithoutResultsAsync();

                return Ok(new { Message = "Comment created successfully", Comment = comment });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // GET: /Comment/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCommentById(string id)
        {
            try
            {
                var query = @"
                    MATCH (c:Comment {commentId: $commentId})-[:BELONGS_TO]->(p:Post), (u:User)-[:AUTHORED]->(c)
                    RETURN c, p, u";

                var result = await _graphClient.Cypher
                    .WithParam("commentId", id)
                    .Return((c, p, u) => new
                    {
                        Comment = c.As<Comment>(),
                        Post = p.As<Post>(),
                        Author = u.As<User>()
                    })
                    .ResultsAsync;

                var comment = result.FirstOrDefault();
                if (comment == null)
                {
                    return NotFound(new { Message = "Comment not found" });
                }

                comment.Comment.author = comment.Author;
                comment.Comment.post = comment.Post;

                return Ok(comment.Comment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // GET: /Comment/Post/{postId}
        [HttpGet("Post/{postId}")]
        public async Task<IActionResult> GetCommentsByPostId(string postId)
        {
            try
            {
                var query = @"
                    MATCH (c:Comment)-[:BELONGS_TO]->(p:Post {postId: $postId}), (u:User)-[:AUTHORED]->(c)
                    RETURN c, u";

                var results = await _graphClient.Cypher
                    .WithParam("postId", postId)
                    .Return((c, u) => new
                    {
                        Comment = c.As<Comment>(),
                        Author = u.As<User>()
                    })
                    .ResultsAsync;

                var comments = results.Select(r =>
                {
                    r.Comment.author = r.Author;
                    return r.Comment;
                }).ToList();

                return Ok(comments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // PUT: /Comment/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(string id, [FromBody] Comment updatedComment)
        {
            try
            {
                var query = @"
                    MATCH (c:Comment {commentId: $commentId})
                    SET c.content = $content
                    RETURN c";

                var parameters = new
                {
                    commentId = id,
                    content = updatedComment.content
                };

                var result = await _graphClient.Cypher
                    .WithParams(parameters)
                    .Return(c => c.As<Comment>())
                    .ResultsAsync;

                var comment = result.FirstOrDefault();
                if (comment == null)
                {
                    return NotFound(new { Message = "Comment not found" });
                }

                return Ok(new { Message = "Comment updated successfully", Comment = comment });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // DELETE: /Comment/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(string id)
        {
            try
            {
                var query = @"
                    MATCH (c:Comment {commentId: $commentId})
                    DETACH DELETE c";

                await _graphClient.Cypher
                    .WithParam("commentId", id)
                    .ExecuteWithoutResultsAsync();

                return Ok(new { Message = "Comment deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
