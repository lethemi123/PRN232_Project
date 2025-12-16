using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectPRN232.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/upload")]
    [Authorize(Roles = "Admin")]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        public UploadController(IWebHostEnvironment env) => _env = env;

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "File empty" });

            var ext = Path.GetExtension(file.FileName).ToLower();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Invalid file type" });

            var fileName = $"{Guid.NewGuid()}{ext}";
            var folder = Path.Combine(_env.WebRootPath, "images", "products");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            // URL trả cho FE
            var imageUrl = $"/images/products/{fileName}";
            return Ok(new { imageUrl });
        }
    }
}
