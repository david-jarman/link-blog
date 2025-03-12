using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageMagick;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkBlog.Web.Controllers
{
    [Route("api")]
    [Authorize(Policy = "Admin")]
    public class UploadImageController : Controller
    {
        private readonly BlobServiceClient blobServiceClient;

        public UploadImageController(BlobServiceClient blobServiceClient)
        {
            this.blobServiceClient = blobServiceClient;
        }

        [HttpPost("upload")]
        [RequireAntiforgeryToken(required: false)]
        public async Task<ActionResult> UploadAsync(IFormFile file, CancellationToken ct)
        {
            if (file is null)
            {
                return BadRequest();
            }

            // Get or create the container first. Container name is "images".
            var containerClient = blobServiceClient.GetBlobContainerClient("images");
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);

            // Blob path should be prefixed with the current datetime to ensure uniqueness.
            // Example: "2025/08/01/12/00/00/imagename.png"
            string blobPath = $"{DateTimeOffset.UtcNow:yyyy/MM/dd/HH/mm/ss}/{fileNameWithoutExtension}.png";

            var blobClient = containerClient.GetBlobClient(blobPath);

            // Check if the blob already exists. If it does, return a 409 Conflict.
            if (await blobClient.ExistsAsync(ct))
            {
                return Conflict();
            }

            // Load the image from the stream.
            using Stream stream = file.OpenReadStream();
            using MagickImage image = new(stream);

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
            using MemoryStream processedImage = new MemoryStream();
            image.Write(processedImage);
            processedImage.Position = 0;

            var response = await blobClient.UploadAsync(processedImage, true, ct);

            // Check if the response was successful. If it was, return the permanent url to the blob.
            if (response.GetRawResponse().Status == StatusCodes.Status201Created)
            {
                return Created(blobClient.Uri.AbsoluteUri, null);
            }
            else
            {
                // If the response was not successful, return a 500 Internal Server Error.
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
