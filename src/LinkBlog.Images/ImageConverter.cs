
using ImageMagick;

namespace LinkBlog.Images;

public class ImageConverter : IImageConverter
{
    public async Task<Stream> ConvertToPngAsync(Stream originalImage, CancellationToken ct)
    {
        using MagickImage image = new(originalImage);

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
        }

        // Set the output format to PNG.
        image.Format = MagickFormat.Png;

        // Write the result to a memory stream.
        MemoryStream processedImage = new MemoryStream();
        await image.WriteAsync(processedImage, ct);

        // Set the stream position back to 0
        processedImage.Position = 0;

        return processedImage;
    }
}
