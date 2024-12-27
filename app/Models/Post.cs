using Neo4j.Driver;
using Neo4jClient.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace app.Models
{
    public class Post
    {
        public String postId { get; set; }
        public String imageURL { get; set; }
        public String caption { get; set; }

        [JsonConverter(typeof(LocalDateTimeConverter))]
        public LocalDateTime? createdAt { get; set; }
        public String author { get; set; }
        public List<User> likes { get; set; }=new List<User>();
        public int likeCount { get; set; }

        public Post()
        {
        }
        public Post(String postId, String imageURL, String caption, LocalDateTime createdAt, String author, int lk)
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
public class LocalDateTimeConverter : JsonConverter<LocalDateTime>
{
    public override LocalDateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateTime = DateTime.Parse(reader.GetString());
        return new LocalDateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
    }

    public override void Write(Utf8JsonWriter writer, LocalDateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}