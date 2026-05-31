using System.Runtime.CompilerServices;
using Azure.Storage.Blobs;
using LinkBlog.Abstractions;
using Microsoft.Extensions.Logging;

namespace LinkBlog.Data;

/// <summary>
/// Azure Blob Storage-backed implementation of IPostDataAccess.
/// Each post is a markdown file with YAML frontmatter stored in the "posts" blob container.
/// </summary>
public sealed class MarkdownPostDataAccess : IPostDataAccess
{
    private const string ContainerName = "posts";
    private readonly BlobContainerClient containerClient;
    private readonly PostMarkdownSerializer serializer;
    private readonly ILogger<MarkdownPostDataAccess> logger;

    public MarkdownPostDataAccess(BlobServiceClient blobServiceClient, PostMarkdownSerializer serializer, ILogger<MarkdownPostDataAccess> logger)
    {
        this.containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        this.serializer = serializer;
        this.logger = logger;
    }

    public async IAsyncEnumerable<Post> GetAllPostsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        await foreach (var blobItem in this.containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var blobClient = this.containerClient.GetBlobClient(blobItem.Name);
            var response = await blobClient.DownloadContentAsync(cancellationToken);
            var content = response.Value.Content.ToString();

            Post? post = null;

            try
            {
                post = this.serializer.Deserialize(content);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to parse post.");
                continue;
            }

            if (post is not null && !post.IsArchived)
            {
                yield return post;
            }
        }
    }
}