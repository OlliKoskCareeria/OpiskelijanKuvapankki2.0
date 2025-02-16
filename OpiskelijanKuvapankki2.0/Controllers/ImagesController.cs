using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpiskelijanKuvapankki2_0.Models;
using Microsoft.EntityFrameworkCore;
using static System.Net.WebRequestMethods;
namespace OpiskelijanKuvapankki2_0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController(OpiskelijanKuvapankki2_0Context _db) : ControllerBase
    {
        private readonly OpiskelijanKuvapankki2_0Context db = _db;

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
        public async Task<ActionResult<IEnumerable<Image>>> HaeKaikkiKuvat()
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
    }
}
