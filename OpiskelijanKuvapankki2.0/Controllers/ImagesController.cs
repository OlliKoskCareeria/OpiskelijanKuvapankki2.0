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
    public class ImagesController(OpiskelijanKuvapankki2_0Context _db, ImageService _imageservice) : ControllerBase
    {
        private readonly OpiskelijanKuvapankki2_0Context db = _db;
        private readonly ImageService imageservice = _imageservice;


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

        [HttpGet]
        [AllowAnonymous]//salli kirjautumaton käyttäjä
        public async Task<ActionResult<IEnumerable<Models.Image>>> GetAllImages()
        {

            var images = await db.Images.Include(c => c.Category).ToListAsync();

            var logins = await db.Logins.ToListAsync();


            var imageDetails = images.Select(image => new ImageDetails //Luodaan kuvadetails olio
            {
                ImageId = image.ImageId,
                ImageName = image.ImageName,
                Category = image.Category?.CategoryName,
                Photographer = logins.FirstOrDefault(k => k.LoginId == image.LoginId)?.Name,
                Contact = logins.FirstOrDefault(l => l.LoginId == image.LoginId)?.Contact,
                ImageLink = image.ImageLink
            }).ToList();
            return Ok(imageDetails);
        }


        [HttpPost("Upload")]
        [Consumes("multipart/form-data")] //ottaa vastaan lomakkeen
        public async Task<ActionResult<Models.Image>> AddNew
            (
            [FromForm] int LoginId,
            [FromForm] CategoryType CategoryId,
            [FromForm] string ImageName,
            IFormFile ImageBytes)
        {
            var imageCount = db.Images.Count();
            List<string> allowedFileTypes = new List<string> { "image/jpeg", "image/png", "image/jpg", "image/webp" };
            var form = await Request.ReadFormAsync(); //tallennetaan lomake muuttujaan
            var file = ImageBytes; //tallennetaan tiedosto muuttujaan

            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Tiedostoa ei löytynyt");
                }
                if (!allowedFileTypes.Contains(file.ContentType))
                {
                    return BadRequest(new { Message = "Tätä tiedostoa ei voida tallentaa. Sallitut tiedostomuodot ovat jpg,jpeg,png,webp" });
                }
                if (imageCount > 1000)
                {
                    return BadRequest(new { Message = "Kuvapankin enimmäiskoko on ylitetty eikä kuvaa voitu ladata palveluun." });
                }

                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream); //kopioidaan tiedoston sisältö
                    memoryStream.Seek(0, SeekOrigin.Begin); //palautetaan streami alkuun
                    var binaryfile = await imageservice.DowngradeImageAsync(memoryStream);//Palauttaa muokatun tiedoston

                    var url = Url.Action("ReturnImage", "Images", new { ImageName }, Request.Scheme); //api end point joka palauttaa kuvan

                    var getidbasedonname = await db.Images.FirstOrDefaultAsync(i => i.Category.CategoryName == form["CategoryId"].ToString());


                    var newimage = new Models.Image  //Muodostetaan kuvaolio
                    {
                        ImageName = form["ImageName"].ToString(),
                        ImageLink = url,
                        ImageBytes = binaryfile,
                        CategoryId = getidbasedonname.CategoryId,
                        LoginId = int.Parse(form["LoginId"].ToString()),

                    };

                    db.Images.Add(newimage);
                    await db.SaveChangesAsync();



                    return Ok($"Lisättiin uusi kuva {newimage.ImageName}");
                    
                }
            }
            catch (Exception e)
            {
                return BadRequest("Tapahtui virhe. Lue lisää: " + e.InnerException);
            }
        }

    }
}
