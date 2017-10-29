using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miniblog.Core.Models
{
    public class PostsViewModel : IEnumerable<Post>
    {
        private IEnumerable<Post> _posts { get; set; }

        public PostsViewModel(IEnumerable<Post> posts, int count)
        {
            _posts = posts;
            Count = count;
        }

        public int Count { get; set; }

        public IEnumerator<Post> GetEnumerator()
        {
            return _posts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _posts.GetEnumerator();
        }
    }
}
