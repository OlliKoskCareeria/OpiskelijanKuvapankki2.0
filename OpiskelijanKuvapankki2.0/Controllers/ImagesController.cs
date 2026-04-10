using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpiskelijanKuvapankki2_0.Dtos.ImageDtos;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services;
using OpiskelijanKuvapankki2_0.Extensions;
using SixLabors.ImageSharp;
using static System.Net.WebRequestMethods;
using System.Security.Claims;
namespace OpiskelijanKuvapankki2_0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController(ImageSharpService _imagesharpservice, ImageService _imageService) : ControllerBase
    {
        
        private readonly ImageSharpService imagesharpservice = _imagesharpservice;
        private readonly ImageService imageservice = _imageService;


        [HttpGet("/kuva/{imagename}")]
        [AllowAnonymous]
        public async Task<IActionResult> ReturnImage(string imagename)//Api end point Palauttaa kuvan
        {
            var image = await imageservice.GetImageByNameAsync(imagename);
            if (image == null||image.ImageBytes == null)
            {
                return NotFound();
            }

            return File(image.ImageBytes, "image/jpeg", $"{image.ImageName}.jpeg");
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Models.Image>> GetImageById(int id)
        {
            var image = await imageservice.GetImageByIdAsync(id);
            if (image == null)
            {
                return NotFound();
            }

            return image;
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteImage(int id)
        {
            var result = imageservice.DeleteImage(id);
            if (!result.Success)
            {
                return result.Message.Contains("ei löytynyt")
                    ? NotFound(result.Message)
                    : BadRequest(result.Message);
            }
            return Ok(result);
        }

        [HttpGet]
        
        public async Task<ActionResult<IEnumerable<Models.Image>>> GetAllImages()
        {
            var imageDetails = await imageservice.GetAllImagesAsync();

            return Ok(imageDetails);
        }

        [Authorize]
        [HttpPost("Upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddNew([FromForm] UploadImageDto dto)
        {
            var loginId = User.GetUserId();

            var result = await _imageService.AddNewImageAsync(
                loginId,
                dto.CategoryName,
                dto.ImageName,
                dto.ImageFile
            );

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        


        [HttpGet("download/{id}")]
        public async Task<IActionResult> LoadImage(int id)
        {
            var response = await imageservice.LoadImageAsync(id);
            if (response.Image == null)
            {
                return NotFound();
            }

            return File(response.Image, "application/octet-stream", response.Message);//palautetaan tunnistamaton binääritiedosto. Tiedosto nimetään ja määritetään tiedostopäätteellä sen tyyppi
        }

        [HttpGet("downloadresized/{id}")]
        public async Task<IActionResult> LoadResizedImage(int id, int height = 700, int quality = 80)
        {
            var result = await imageservice.LoadImageAsync(id);

            if (result.Image == null)
            {
                return NotFound();
            }


            var ResizedFile = imagesharpservice.SetSizeAndQualityImage(result.Image, height, quality);//Käyttäjällä on mahdollisuus muuttaa kuvan kokoa ja laatua
            
            return File(ResizedFile, "application/octet-stream", result.Message);


        }

        [HttpGet("CategoryName/{cname}")]
        [AllowAnonymous]
        public async Task<ActionResult> GetImagesByCategory(string cname)
        {

            try
            {
                var imagesbycategory = await imageservice.GetImagesByCategoryAsync(cname);
                return Ok(imagesbycategory);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Email/{email}")]
        
        public async Task<ActionResult> GetImagesByUser(string email)
        {

               var imagedetails = await imageservice.GetImagesByUserAsync(email);

                if (imagedetails == null) 
                { 
                    return NotFound();
                }

                return Ok(imagedetails);
            
            
        }

    }
}
