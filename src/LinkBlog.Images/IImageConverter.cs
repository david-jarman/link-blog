namespace LinkBlog.Images;

public interface IImageConverter
{
    Task<ProcessedImage> ProcessImageAsync(Stream originalImage, long originalImageSize, CancellationToken ct);
}

public record ProcessedImage(Stream Stream, string ContentType, string FileExtension) : IDisposable
{
    public void Dispose()
    {
        Stream?.Dispose();
        GC.SuppressFinalize(this);
    }
}