using app.Models;
using System.Text.Json.Serialization;

public class Comment
{
    [JsonPropertyName("commentId")]
    public string CommentId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime? CreatedAt { get; set; }

    // Ove atribute možete ostaviti praznim ako nisu deo odgovora iz baze
    public User Author { get; set; }
    public Post Post { get; set; }

    public Comment() { }

    public Comment(string commentId, string content, DateTime createdAt, User author, Post post)
    {
        CommentId = commentId;
        Content = content;
        CreatedAt = createdAt;
        Author = author;
        Post = post;
    }
}
