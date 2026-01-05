
using System.Diagnostics;
using ImageMagick;

namespace LinkBlog.Images;

public class ImageConverter : IImageConverter
{
    public const string ActivitySourceName = "LinkBlog.Images.ImageConversion";

    private static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName);

    public async Task<ProcessedImage> ProcessImageAsync(Stream originalImage, long originalImageSize, CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("ProcessImage", ActivityKind.Internal);
        activity?.SetTag("operation", "process");
        activity?.SetTag("original-image-size", originalImageSize);
        activity?.AddEvent(new ActivityEvent("ImageProcessingStarted"));

        // First, detect if this is a GIF by reading the format
        MagickFormat detectedFormat;
        using (MagickImage tempImage = new(originalImage))
        {
            detectedFormat = tempImage.Format;
        }

        // Reset stream position after detection
        originalImage.Position = 0;

        bool isGif = detectedFormat == MagickFormat.Gif;
        activity?.SetTag("is-gif", isGif);
        activity?.AddEvent(new ActivityEvent("ImageProcessingFormatDetected"));

        MemoryStream processedImage = new MemoryStream();
        string format;
        string contentType;
        string fileExtension;

        if (isGif)
        {
            // Preserve GIF as-is without any processing
            using MagickImageCollection collection = new(originalImage);
            activity?.AddEvent(new ActivityEvent("ImageProcessingGifCollectionLoaded"));
            activity?.SetTag("frame-count", collection.Count);

            // Write the GIF to the stream without modification
            await collection.WriteAsync(processedImage, ct);

            format = "GIF";
            contentType = "image/gif";
            fileExtension = ".gif";
            activity?.AddEvent(new ActivityEvent("ImageProcessingPreservedAsAnimatedGif"));
        }
        else
        {
            // For non-GIF images, use single image processing
            using MagickImage image = new(originalImage);
            activity?.AddEvent(new ActivityEvent("ImageProcessingMagickImageLoaded"));

            // Store the ICC profile for later use (if present)
            var icc = image.GetProfile("icc");

            // Remove all metadata from the image
            image.Strip();
            if (icc != null)
            {
                // Add the ICC profile back to the image
                image.SetProfile(icc);
            }

            // If the image width is greater than 2000px, resize it
            if (image.Width > 2000)
            {
                // Resizing with height set to 0 preserves the aspect ratio
                image.Resize(2000, 0);
                activity?.AddEvent(new ActivityEvent("ImageProcessingResized"));
            }

            // Convert to PNG for other formats
            image.Format = MagickFormat.Png;
            format = "PNG";
            contentType = "image/png";
            fileExtension = ".png";
            activity?.AddEvent(new ActivityEvent("ImageProcessingConvertedToPng"));

            // Write the result to a memory stream
            await image.WriteAsync(processedImage, ct);
        }

        // Set the stream position back to 0
        processedImage.Position = 0;

        activity?.SetTag("processed-image-size", processedImage.Length);
        activity?.SetTag("output-format", format);
        activity?.AddEvent(new ActivityEvent("ImageProcessingFinished"));

        return new ProcessedImage(processedImage, contentType, fileExtension);
    }
}