namespace RaceDay1.Services
{//start of namespace

    // PART 3: saves uploaded images (event banners, profile pictures) on the API server,
    // inside wwwroot/uploads, and returns a URL the web app can show in an <img> tag.
    public class ImageStorageService
    {//start of class
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _http;

        public ImageStorageService(IWebHostEnvironment env, IHttpContextAccessor http)
        {
            _env = env;
            _http = http;
        }

        // Saves the file and returns its public URL, e.g. http://localhost:5173/uploads/banners/abc.jpg
        // 'folder' separates the file types: "banners" or "profiles".
        public async Task<string> SaveImageAsync(IFormFile file, string folder)
        {
            string root = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            string directory = Path.Combine(root, "uploads", folder);
            Directory.CreateDirectory(directory);

            // A GUID in the name stops two files overwriting each other.
            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName).ToLowerInvariant();

            await using (var stream = File.Create(Path.Combine(directory, fileName)))
            {
                await file.CopyToAsync(stream);
            }

            var request = _http.HttpContext!.Request;
            return $"{request.Scheme}://{request.Host}/uploads/{folder}/{fileName}";
        }

        // Shared check used by both upload endpoints: real image type, size under 5MB.
        public static bool IsValidImage(IFormFile? file) =>
            file != null && file.Length > 0 && file.Length <= 5_000_000 &&
            file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) &&
            AllowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());
    }//end of class
}//end of namespace
