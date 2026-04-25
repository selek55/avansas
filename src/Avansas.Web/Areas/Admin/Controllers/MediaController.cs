using Avansas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Avansas.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
[Route("admin/media")]
public class MediaController : Controller
{
    private readonly IProductService _productService;
    private readonly IFileService _fileService;

    public MediaController(IProductService productService, IFileService fileService)
    {
        _productService = productService;
        _fileService = fileService;
    }

    [HttpPost("upload/{productId:int}")]
    public async Task<IActionResult> Upload(int productId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Dosya seçilmedi" });

        if (!_fileService.IsValidImageFile(file.FileName, file.Length))
            return BadRequest(new { error = "Geçersiz dosya. Max 10MB, desteklenen: jpg, jpeg, png, webp, gif" });

        try
        {
            await using var stream = file.OpenReadStream();
            var url = await _fileService.SaveFileAsync(stream, file.FileName, "images/products");

            // ilk resimse ana görsel yap
            var existing = await _productService.GetProductImagesAsync(productId);
            var setMain = existing.Count == 0 || !existing.Any(i => i.IsMain);

            var image = await _productService.AddProductImageAsync(productId, url, isMain: setMain);
            return Ok(new { id = image.Id, url = image.ImageUrl, isMain = image.IsMain, displayOrder = image.DisplayOrder });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("delete/{imageId:int}")]
    public async Task<IActionResult> Delete(int imageId, [FromQuery] string? fileUrl)
    {
        try
        {
            if (!string.IsNullOrEmpty(fileUrl))
                await _fileService.DeleteFileAsync(fileUrl);

            await _productService.DeleteProductImageAsync(imageId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("set-main/{productId:int}/{imageId:int}")]
    public async Task<IActionResult> SetMain(int productId, int imageId)
    {
        try
        {
            await _productService.SetMainImageAsync(productId, imageId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("reorder/{productId:int}")]
    public async Task<IActionResult> Reorder(int productId, [FromBody] List<int> imageIds)
    {
        try
        {
            await _productService.ReorderImagesAsync(productId, imageIds);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("list/{productId:int}")]
    public async Task<IActionResult> List(int productId)
    {
        var images = await _productService.GetProductImagesAsync(productId);
        return Ok(images);
    }

}
