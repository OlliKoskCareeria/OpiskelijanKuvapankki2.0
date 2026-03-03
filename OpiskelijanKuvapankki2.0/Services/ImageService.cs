using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpiskelijanKuvapankki2_0.Models;


namespace OpiskelijanKuvapankki2_0.Services
{
    public class ImageService(OpiskelijanKuvapankki2_0Context _db, ILogger<ImageService> _logger)
    {
        private readonly OpiskelijanKuvapankki2_0Context db = _db;
        private readonly ILogger<ImageService> logger = _logger;
        public VerificationResult DeleteImage(int id)
        {
            VerificationResult result = new();
            try
            {
                var img = db.Images.Find(id);
                if (img == null)
                {
                    result.Success = false;
                    result.Message = "Kuvaa ei löytynyt.";
                    return result;
                }

                db.Images.Remove(img);
                db.SaveChanges();

                result.Success = true;
                result.Message = $"{img.ImageName} poistettiin.";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Poisto epäonnistui.";
                logger.LogError(ex, "Kuvan poistaminen epäonnistui Id {ImageId}", id);
                return result;
            }
        }

        public async Task<List<ImageDetails>> GetAllImagesAsync()
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

            return imageDetails;
        }
    }
}
