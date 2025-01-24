namespace app.Models
{
    public class Like
    {
        public User user { get; set; }
        public Post post { get; set; }
       // public Comment comment { get; set; } // Dodajte comment

        public Like(User user, Post post) { 
            this.user = user;
            this.post = post;
            //this.comment = comment; // Omogućite komentar kao opcioni parametar
        }
    }
}
