namespace LinkBlog.Images;

public interface IImageConverter
{
    Task<Stream> ConvertToPngAsync(Stream originalImage, long originalImageSize, CancellationToken ct);
}
