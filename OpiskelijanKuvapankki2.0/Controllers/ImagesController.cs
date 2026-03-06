using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services;
using SixLabors.ImageSharp;
using static System.Net.WebRequestMethods;
namespace OpiskelijanKuvapankki2_0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController(OpiskelijanKuvapankki2_0Context _db, ImageSharpService _imagesharpservice, ImageService _imageService) : ControllerBase
    {
        private readonly OpiskelijanKuvapankki2_0Context db = _db;
        private readonly ImageSharpService imagesharpservice = _imagesharpservice;
        private readonly ImageService imageservice = _imageService;


        [HttpGet("/kuva/{imagename}")]
        [AllowAnonymous]
        public async Task<IActionResult> ReturnImage(string imagename)//Api end point Palauttaa kuvan
        {
            var image = await db.Images.FirstOrDefaultAsync(i => i.ImageName == imagename);
            if (image == null)
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
        [AllowAnonymous]//salli kirjautumaton käyttäjä
        public async Task<ActionResult<IEnumerable<Models.Image>>> GetAllImages()
        {
            var imageDetails = await imageservice.GetAllImagesAsync();

            return Ok(imageDetails);
        }


        [HttpPost("Upload")]
        [Consumes("multipart/form-data")] 
        public async Task<ActionResult<Models.Image>> AddNew
            (
        [FromForm] int LoginId,
        [FromForm] string CategoryName,
        [FromForm] string ImageName,
        IFormFile ImageBytes
            )
        {
            var form = await Request.ReadFormAsync();
            var result = await _imageService.AddNewImageAsync(LoginId, form["CategoryName"], form["ImageName"], ImageBytes);

            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

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
            var image = await db.Images.FindAsync(id);


            if (image == null)
            {
                return NotFound();
            }

            var ByteFile = image.ImageBytes;

            if (ByteFile == null)
            {
                return NotFound();
            }

            var ResizedFile = imagesharpservice.SetSizeAndQualityImage(ByteFile, height, quality);//Käyttäjällä on mahdollisuus muuttaa kuvan kokoa ja laatua
            var FileName = image.ImageName + ".jpg" ?? "ladattu_kuva.jpg";//fallbackin pitäisi olla turha koska upload vaatii nimeämään kuvan

            return File(ResizedFile, "application/octet-stream", FileName);


        }

    }
}
