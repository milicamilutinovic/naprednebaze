using Neo4j.Driver;
using System;

namespace app.Models
{
    public class User
    {
        public string? UserId { get; set; }  // This will be generated automatically
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? ProfilePicture { get; set; }
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAdmin { get; set; }

        public List<Post>? postovi { get; set; }


        // Parameterless constructor (Required for proper deserialization)
        public User() { }

        //// Constructor for initialization if needed
        public User(string username, string fullName, string email, string passwordHash,
                    string profilePicture, string bio, DateTime createdAt, bool isAdmin)
        {
            Username = username;
            FullName = fullName;
            Email = email;
            PasswordHash = passwordHash;
            ProfilePicture = profilePicture;
            Bio = bio;
            CreatedAt = createdAt;
            IsAdmin = isAdmin;
        }

        public bool isAdmin()
        {
            return IsAdmin;
        }
    }


}
