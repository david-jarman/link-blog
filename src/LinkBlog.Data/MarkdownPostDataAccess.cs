using System.Runtime.CompilerServices;
using Azure.Storage.Blobs;
using LinkBlog.Abstractions;

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

    public MarkdownPostDataAccess(BlobServiceClient blobServiceClient, PostMarkdownSerializer serializer)
    {
        this.containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        this.serializer = serializer;
    }

    public async IAsyncEnumerable<Post> GetAllPostsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await this.containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        await foreach (var blobItem in this.containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var blobClient = this.containerClient.GetBlobClient(blobItem.Name);
            var response = await blobClient.DownloadContentAsync(cancellationToken);
            var content = response.Value.Content.ToString();

            var post = this.serializer.Deserialize(content);
            if (!post.IsArchived)
            {
                yield return post;
            }
        }
    }
}