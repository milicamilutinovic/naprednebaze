using app.Models;
using System;

public class Comment
{
    public string CommentId { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public User Author { get; set; }
    public Post Post { get; set; }

    // Podrazumevani konstruktor (neophodan za deserializaciju)
    public Comment() { }

    // Konstruktor sa parametrima (ako je potrebno)
    public Comment(string commentId, string content, DateTime createdAt, User author, Post post)
    {
        CommentId = commentId;
        Content = content;
        CreatedAt = createdAt;
        Author = author;
        Post = post;
    }
}
