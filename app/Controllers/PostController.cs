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
    public class PostController : Controller
    {
        private readonly IGraphClient _graphClient;
        
        public PostController(IGraphClient graphClient)
        {
            _graphClient = graphClient;
        }

        // POST: /Post
        [HttpPost("addPost")]
        public async Task<IActionResult> CreatePost1([FromBody] Post post)
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


                var cypherQuery = @"
MATCH (u:User {userId: $userId})
CREATE (p:Post {postId: $postId, imageURL: $imageURL, caption: $caption, createdAt: $createdAt, likeCount: $likeCount})
CREATE (u)-[:CREATED]->(p)
CREATE (u)-[:AUTHORED_BY]->(p)  // Dodajte vezu između autora i posta
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
        [HttpPost("CreatePost")]
        public async Task<IActionResult> CreatePost([FromForm] IFormFile image, [FromForm] string caption)
        {
            try
            {
                // Validacija podataka
                if (image == null || string.IsNullOrEmpty(caption))
                {
                    return BadRequest(new { error = "Image and caption are required." });
                }

                // var userId = "c01db770-0cc9-43e4-b317-5c10f0866164"; // Fetch the currently logged-in user
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null)
                {
                    return Unauthorized(new { error = "User is not logged in." });
                }

                var userId = userIdClaim.Value;
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", image.FileName);

                // Save the image to the server
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Kreiranje posta
                var post = new Post
                {
                    postId = Guid.NewGuid().ToString(),
                    imageURL = "/images/" + image.FileName,
                    caption = caption,
                    createdAt = DateTime.Now,
                    author = userId,  // Assuming user.Username exists
                    likeCount = 0
                };

                // Spajanje sa Neo4j bazom
                var cypherQuery = @"
MATCH (u:User {userId: $userId})
CREATE (p:Post {postId: $postId, imageURL: $imageURL, caption: $caption, createdAt: $createdAt, likeCount: $likeCount,author: $author})
CREATE (u)-[:CREATED]->(p)
RETURN p";

                var parameters = new
                {
                    postId = post.postId,
                    imageURL = post.imageURL,
                    caption = post.caption,
                    createdAt = post.createdAt,
                    likeCount = post.likeCount,
                    author=post.author, // Assuming user.UserId exists
                    userId = userId
                };

                // Izvršavanje upita
                var result = await _graphClient.Cypher
           .WithParams(parameters)
           .Match("(u:User {userId: $userId})")
           .Create("(p:Post {postId: $postId, imageURL: $imageURL, caption: $caption, createdAt: $createdAt, likeCount: $likeCount, author: $author})")
           .Create("(u)-[:CREATED]->(p)")
           .Return(p => p.As<Post>())
           .ResultsAsync;

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
                var query = _graphClient.Cypher
    .Match("(p:Post)<-[:CREATED]-(u:User)")  // Povezivanje sa korisnikom
    .Where((Post p) => p.postId == postId)
    .Return((p, u) => new { Post = p.As<Post>(), Author = u.As<User>() }); // Vraćanje posta i autora

                var result = await query.ResultsAsync;

                var post = result.FirstOrDefault();
                if (post == null)
                {
                    return NotFound($"Post with ID {postId} not found.");
                }

                // Vraćanje posta sa autorom
                post.Post.author = post.Author?.UserId; // Dodajte author iz korisničkog nod-a ako je pronađen
                return Ok(post.Post); // Vraća post sa autorom
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        // GET: /Post
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



