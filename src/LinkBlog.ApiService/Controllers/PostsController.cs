using LinkBlog.ApiService;
using LinkBlog.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private IPostStore postStore { get; }

        public PostsController(IPostStore postStore)
        {
            this.postStore = postStore;
        }

        [HttpGet]
        public IActionResult GetPosts(int page = 0)
        {
            var posts = postStore.GetPosts().Skip(page * 10).Take(10);

            return Ok(posts);
        }

        [HttpGet]
        [Route("tag/{tag}")]
        [ProducesResponseType(200)]
        public IAsyncEnumerable<Post> GetPostsForTag(string tag, CancellationToken ct)
        {
            return postStore.GetPostsForTag(tag, ct);
        }
    }
}
