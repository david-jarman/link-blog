
using System.Diagnostics;
using ImageMagick;

namespace LinkBlog.Images;

public class ImageConverter : IImageConverter
{
    public const string ActivitySourceName = "LinkBlog.Images.ImageConversion";

    private static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName);

    public async Task<Stream> ConvertToPngAsync(Stream originalImage, long originalImageSize, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("ConvertImage", ActivityKind.Internal);
        activity?.SetTag("operation", "convert");
        activity?.SetTag("original-image-size", originalImageSize);
        activity?.AddEvent(new ActivityEvent("ImageConversionStarted"));

        using MagickImage image = new(originalImage);
        activity?.AddEvent(new ActivityEvent("ImageConversionMagickImageLoaded"));

        // Store the ICC profile for later use.
        var icc = image.GetProfile("icc");

        // Remove all metadata from the image.
        image.Strip();
        if (icc != null)
        {
            // Add the ICC profile back to the image.
            image.SetProfile(icc);
        }

        // If the image width is greater than 2000px, resize it
        if (image.Width > 2000)
        {
            // Resizing with height set to 0 preserves the aspect ratio.
            image.Resize(2000, 0);
            activity?.AddEvent(new ActivityEvent("ImageConversionResized"));
        }

        // Set the output format to PNG.
        image.Format = MagickFormat.Png;

        // Write the result to a memory stream.
        MemoryStream processedImage = new MemoryStream();
        await image.WriteAsync(processedImage, ct);

        // Set the stream position back to 0
        processedImage.Position = 0;

        activity?.SetTag("processed-image-size", processedImage.Length);
        activity?.AddEvent(new ActivityEvent("ImageConversionFinished"));

        return processedImage;
    }
}