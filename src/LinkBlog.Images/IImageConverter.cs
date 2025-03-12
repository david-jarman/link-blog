namespace LinkBlog.Images;

public interface IImageConverter
{
    Task<Stream> ConvertToPngAsync(Stream originalImage, CancellationToken ct);
}
