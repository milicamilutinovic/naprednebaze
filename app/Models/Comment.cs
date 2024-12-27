using Neo4j.Driver;

namespace app.Models
{ 

    public class Comment
    {
        public String commentId { get; set; }
        public String content { get; set; }
        public LocalDateTime createdAt { get; set; }
        public User author { get; set; }
        public Post post { get; set; }


        public Comment(String commentId, String content, LocalDateTime createdAt, User author, Post post)
        {
            this.commentId = commentId;
            this.content = content;
            this.createdAt = createdAt;
            this.author = author;
            this.post = post;
        }

        

    }

}
