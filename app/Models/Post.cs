using Neo4j.Driver;

namespace app.Models
{
    public class Post
    {
        public String postId { get; set; }
        public String imageURL { get; set; }
        public String caption { get; set; }
        public LocalDateTime createdAt { get; set; }
        public User author { get; set; }
        public List<User> likes { get; set; }
        public int likeCount { get; set; }

        public Post(String postId, String imageURL, String caption, LocalDateTime createdAt, User author, int lk)
        {
            this.postId = postId;
            this.imageURL = imageURL;
            this.caption = caption;
            this.createdAt = createdAt;
            this.author = author;
            this.likeCount = lk;
        }

        //public void like(User user)
        //{
        //    likes.add(user);
        //    likeCount = likes.size(); 
        //}

        //public void unlike(User user)
        //{
        //    likes.remove(user);
        //    likeCount = likes.size(); 
        //}


    }

}
