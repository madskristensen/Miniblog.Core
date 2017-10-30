namespace Miniblog.Core.Models
{
    public class PostCategory
    {
        public string CategoryID { get; set; }
        public virtual Category Category { get; set; }
        
        public string PostID { get; set; }
        public virtual Post Post { get; set; }
    }
}