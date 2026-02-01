namespace LinkBlog.Web;

public sealed class BlogOptions
{
    /// <summary>
    /// Gets or sets the number of posts to display per page on the home page.
    /// Default is 10.
    /// </summary>
    public int PostsPerPage { get; set; } = 10;
}