using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using app.Models;

namespace app.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostController : ControllerBase
    {
        private readonly IGraphClient _graphClient;

        public PostController(IGraphClient graphClient)
        {
            _graphClient = graphClient;
        }

        // POST: /Post
        [HttpPost("addPost")]
        public async Task<IActionResult> CreatePost([FromBody] Post post)
        {
            try
            {
                // Validacija ulaznih podataka
                if (post == null || post.author == null || string.IsNullOrEmpty(post.author))
                {
                    return BadRequest(new { error = "Invalid post or author data" });
                }

                // Generisanje ID-a ako nije prosleđen
                if (string.IsNullOrEmpty(post.postId))
                {
                    post.postId = Guid.NewGuid().ToString();
                }

                post.postId = Guid.NewGuid().ToString();


                // Cypher upit za kreiranje posta
                var cypherQuery = @"
            MATCH (u:User {userId: $userId})
            CREATE (p:Post {postId: $postId, imageURL: $imageURL, caption: $caption, createdAt: $createdAt, likeCount: $likeCount})
            CREATE (u)-[:CREATED]->(p)
            RETURN p";

                // Parametri za upit
                var parameters = new
                {
                    postId = post.postId,
                    imageURL = post.imageURL,
                    caption = post.caption,
                    createdAt = post.createdAt,
                    likeCount = post.likeCount,
                    userId = post.author // Koristi UserId iz klase User
                };

                // Izvršavanje upita
                var result = await _graphClient.Cypher
                    .Match("(u:User {userId: $userId})")
                    .WithParams(parameters)
                    .Create("(p:Post {postId: $postId, imageURL: $imageURL, caption: $caption, createdAt: $createdAt, likeCount: $likeCount})")
                    .Create("(u)-[:CREATED]->(p)")
                    .Return(p => p.As<Post>())
                    .ResultsAsync;

                if (result == null)
                {
                    return StatusCode(500, new { error = "Failed to create post. User might not exist." });
                }

                // Vraćanje uspešnog odgovora
                return Ok(new { message = "Post created successfully", post });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // GET: /Post/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostById(string id)
        {
            try
            {
                var query = @"
                    MATCH (p:Post {postId: $postId})<-[:CREATED]-(u:User)
                    RETURN p, u";

                var result = await _graphClient.Cypher
                    .WithParam("postId", id)
                    .Return((p, u) => new
                    {
                        Post = p.As<Post>(),
                        Author = u.As<User>()
                    })
                    .ResultsAsync;

                var post = result.FirstOrDefault();
                if (post == null)
                {
                    return NotFound(new { Message = "Post not found" });
                }

              //  post.Post.author = post.Author;

                return Ok(post.Post);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // GET: /Post
        [HttpGet]
        public async Task<IActionResult> GetAllPosts()
        {
            try
            {
                var query = @"
                    MATCH (p:Post)<-[:CREATED]-(u:User)
                    RETURN p, u";

                var results = await _graphClient.Cypher
                    .Return((p, u) => new
                    {
                        Post = p.As<Post>(),
                        Author = u.As<User>()
                    })
                    .ResultsAsync;

                var posts = new List<Post>();

                foreach (var result in results)
                {
                  //  result.Post.author = result.Author;
                    posts.Add(result.Post);
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // PUT: /Post/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(string id, [FromBody] Post updatedPost)
        {
            try
            {
                var query = @"
                    MATCH (p:Post {postId: $postId})
                    SET p.caption = $caption, p.imageURL = $imageURL
                    RETURN p";

                var parameters = new
                {
                    postId = id,
                    caption = updatedPost.caption,
                    imageURL = updatedPost.imageURL
                };

                var result = await _graphClient.Cypher
                    .WithParams(parameters)
                    .Return(p => p.As<Post>())
                    .ResultsAsync;

                var post = result.FirstOrDefault();
                if (post == null)
                {
                    return NotFound(new { Message = "Post not found" });
                }

                return Ok(new { Message = "Post updated successfully", Post = post });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // DELETE: /Post/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(string id)
        {
            try
            {
                var query = @"
                    MATCH (p:Post {postId: $postId})
                    DETACH DELETE p";

                await _graphClient.Cypher
                    .WithParam("postId", id)
                    .ExecuteWithoutResultsAsync();

                return Ok(new { Message = "Post deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
