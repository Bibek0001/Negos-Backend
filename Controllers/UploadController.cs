using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Diyalo.Api.Services;

namespace Diyalo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly FileUploadService _upload;
    public UploadController(FileUploadService upload) => _upload = upload;

    // POST /api/upload?folder=programs
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string folder = "general")
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "File size must be under 5MB" });

        try
        {
            var url = await _upload.SaveAsync(file, folder);
            return Ok(new { url });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
