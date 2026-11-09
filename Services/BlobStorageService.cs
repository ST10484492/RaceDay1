using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay1.Services
{
    // PART 3: all Azure Blob Storage work lives here.
    // The MVC web app never talks to Azure; it sends the file to the API,
    // and the API calls this service.
    public class BlobStorageService
    {
        private readonly BlobContainerClient _container;

        public BlobStorageService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("AzureStorage:ConnectionString is missing.");

            string containerName = configuration["AzureStorage:ContainerName"] ?? "raceday-images";

            _container = new BlobContainerClient(connectionString, containerName);
        }

        // Uploads an image and returns its public URL.
        // 'folder' separates the file types, e.g. "banners" or "profiles".
        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            // Create the container the first time (blobs are publicly readable
            // so the <img> tags in the web app can display them directly).
            await _container.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // A GUID in the name prevents two files overwriting each other.
            string extension = Path.GetExtension(file.FileName);
            string blobName = $"{folder}/{Guid.NewGuid()}{extension}";

            BlobClient blob = _container.GetBlobClient(blobName);

            await using Stream stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return blob.Uri.ToString();
        }

        // Shared check used by both upload endpoints.
        public static bool IsValidImage(IFormFile? file) =>
            file != null && file.Length > 0 && file.Length <= 5_000_000 &&
            file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}