namespace LinkBlog.Feed;

public class FeedOptions
{
    public int MaxPostCount { get; set; } = 20;
    public string BlogUrl { get; set; } = string.Empty;
    public string BlogTitle { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
}