using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpiskelijanKuvapankki2_0.Models;
using System.Runtime.CompilerServices;


namespace OpiskelijanKuvapankki2_0.Services
{
    public class ImageService(OpiskelijanKuvapankki2_0Context _db, ILogger<ImageService> _logger, ImageSharpService _imageSharpService)
    {
        private readonly OpiskelijanKuvapankki2_0Context db = _db;
        private readonly ILogger<ImageService> logger = _logger;
        private readonly ImageSharpService imageSharpService = _imageSharpService;
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

        public async Task<Image?> GetImageByIdAsync(int id)
        {
            return await _db.Images.FirstOrDefaultAsync(i => i.ImageId == id);
        }

        public async Task<(bool Success, string Message, Image ?Image)> AddNewImageAsync(
        int loginId,
        string categoryName,
        string imageName,
        IFormFile imageBytes)
        {
            var allowedFileTypes = new List<string> { "image/jpeg", "image/png", "image/jpg", "image/webp" };
            var imageCount = await _db.Images.CountAsync();

            try
            {
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    return (false, "Tiedostoa ei löytynyt", null);
                }
                if (!allowedFileTypes.Contains(imageBytes.ContentType))
                {
                    return (false, "Tätä tiedostoa ei voida tallentaa. Sallitut tiedostomuodot ovat jpg,jpeg,png,webp", null);
                }
                if (imageCount > 1000)
                {
                    return (false, "Kuvapankin enimmäiskoko on ylitetty eikä kuvaa voitu ladata palveluun.", null);
                }

                using (var memoryStream = new MemoryStream())
                {
                    await imageBytes.CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var binaryFile = await imageSharpService.DowngradeImageAsync(memoryStream);

                    var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryName.Equals(categoryName));
                    var categoryId = category?.CategoryId;

                    var newImage = new Image
                    {
                        ImageName = imageName,
                        ImageLink = $"api/Images/ReturnImage?ImageName={imageName}",
                        ImageBytes = binaryFile,
                        CategoryId = categoryId,
                        LoginId = loginId,
                    };

                    _db.Images.Add(newImage);
                    await _db.SaveChangesAsync();

                    return (true, $"Lisättiin uusi kuva {newImage.ImageName}", newImage);
                }
            }
            catch (Exception e)
            {
                return (false, $"Tapahtui virhe. Lue lisää: {e.InnerException}", null);
            }
        }

        
    }
}
