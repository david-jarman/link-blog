using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LinkBlog.Images;
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
        private readonly IImageConverter imageConverter;
        private readonly ILogger<UploadImageController> logger;

        public UploadImageController(BlobServiceClient blobServiceClient, IImageConverter imageConverter, ILogger<UploadImageController> logger)
        {
            this.blobServiceClient = blobServiceClient;
            this.imageConverter = imageConverter;
            this.logger = logger;
        }

        [HttpPost("upload")]
        [RequireAntiforgeryToken(required: false)]
        public async Task<ActionResult> UploadAsync(IFormFile file, CancellationToken ct)
        {
            if (file is null)
            {
                logger.LogWarning("No file was uploaded.");
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
                logger.LogWarning("Blob {blobPath} already exists.", blobPath);
                return Conflict();
            }

            // Load the image from the stream.
            using Stream originalImage = file.OpenReadStream();
            using Stream processedImage = await imageConverter.ConvertToPngAsync(originalImage, file.Length, ct);
            var blobUploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "image/png"
                }
            };

            var response = await blobClient.UploadAsync(processedImage, blobUploadOptions, ct);

            // Check if the response was successful. If it was, return the permanent url to the blob.
            if (response.GetRawResponse().Status == StatusCodes.Status201Created)
            {
                logger.LogInformation("Blob {blobPath} successfully created", blobPath);
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
