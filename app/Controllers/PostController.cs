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
        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPostById(string postId)
        {
            try
            {
                // Upit za pronalaženje posta na osnovu postId
                var query = _graphClient.Cypher
                    .Match("(p:Post)")
                    .Where((Post p) => p.postId == postId)
                    .Return(p => p.As<Post>());

                // Izvršavanje upita i dobijanje rezultata
                var posts = await query.ResultsAsync;

                // Proverite da li je post pronađen
                var post = posts.FirstOrDefault(); // Uzima prvi post ili null ako nije pronađen

                if (post == null)
                {
                    return NotFound($"Post with ID {postId} not found.");
                }

                return Ok(post); // Vraća post sa statusom 200 OK
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetPosts([FromQuery] string userId)
        {
            try
            {
                // Ako je userId prosleđen, filtriraj postove tog korisnika
                if (!string.IsNullOrEmpty(userId))
                {
                    var query = _graphClient.Cypher
                        .Match("(u:User)-[:PUBLISHED]->(p:Post)")
                        .Where((User u) => u.UserId == userId)
                        .Return(p => p.As<Post>());

                    var posts = await query.ResultsAsync;

                    return Ok(posts); // Vraća sve postove koji pripadaju korisniku sa userId
                }

                // Ako userId nije prosleđen, vrati sve postove
                var allPostsQuery = _graphClient.Cypher
                    .Match("(p:Post)")
                    .Return(p => p.As<Post>());

                var allPosts = await allPostsQuery.ResultsAsync;

                return Ok(allPosts); // Vraća sve postove
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: /Post/{id}
        [HttpPut("{postId}")]
        public async Task<IActionResult> UpdatePost(string postId, [FromBody] Post updatedPost)
        {
            try
            {
                // Prvo, proverite da li post postoji
                var query = _graphClient.Cypher
                    .Match("(p:Post)")
                    .Where((Post p) => p.postId == postId)
                    .Return(p => p.As<Post>());

                var posts = await query.ResultsAsync;
                var post = posts.FirstOrDefault();

                if (post == null)
                {
                    return NotFound($"Post with ID {postId} not found.");
                }

                // Ažuriranje svojstava postojećeg posta sa novim podacima
                post.caption = updatedPost.caption ?? post.caption;
                post.imageURL = updatedPost.imageURL ?? post.imageURL;
                post.author = updatedPost.author ?? post.author;
                post.createdAt = updatedPost.createdAt != default ? updatedPost.createdAt : post.createdAt;
                post.likeCount = updatedPost.likeCount > 0 ? updatedPost.likeCount : post.likeCount;

                // Ažuriranje u bazi podataka
                await _graphClient.Cypher
                    .Match("(p:Post)")
                    .Where((Post p) => p.postId == postId)
                    .Set("p.caption = {caption}, p.imageURL = {imageURL}, p.author = {author}, p.createdAt = {createdAt}, p.likeCount = {likeCount}")
                    .WithParam("caption", post.caption)
                    .WithParam("imageURL", post.imageURL)
                    .WithParam("author", post.author)
                    .WithParam("createdAt", post.createdAt)
                    .WithParam("likeCount", post.likeCount)
                    .ExecuteWithoutResultsAsync();

                return Ok(post); // Vraća ažurirani post sa statusom 200 OK
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        // DELETE: /Post/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(string id)
        {
            try
            {
                // Proverite da li post postoji
                var postExists = await _graphClient.Cypher
                    .Match("(p:Post {postId: $postId})")
                    .WithParam("postId", id)
                    .Return<int>("count(p)")  // Broj postova sa datim postId
                    .ResultsAsync;

                if (postExists.Single() == 0)
                {
                    return NotFound(new { message = "Post not found" });
                }

                // Ako post postoji, obrišite ga
                var query = _graphClient.Cypher
                    .Match("(p:Post {postId: $postId})")
                    .WithParam("postId", id)
                    .Delete("p")  // Briše čvor posta
                    .ExecuteWithoutResultsAsync();

                await query;

                return NoContent();  // Vraća 204 status kod kada je resurs uspešno obrisan
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

    }
}



