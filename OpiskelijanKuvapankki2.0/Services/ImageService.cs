using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpiskelijanKuvapankki2_0.Dtos.ImageDtos;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Responses;
using System.Runtime.CompilerServices;


namespace OpiskelijanKuvapankki2_0.Services
{
    public class ImageService(OpiskelijanKuvapankki2_0Context _db, ILogger<ImageService> _logger, ImageSharpService _imageSharpService)
    {
        private readonly OpiskelijanKuvapankki2_0Context db = _db;
        private readonly ILogger<ImageService> logger = _logger;
        private readonly ImageSharpService imageSharpService = _imageSharpService;
        public OperationResult DeleteImage(int id)
        {
            OperationResult result = new();
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

        public async Task<List<ImageDetails>?> GetImagesByCategoryAsync(string cname)
        {
            var imageDetails = await (
                from image in db.Images
                join login in db.Logins
                on image.LoginId equals login.LoginId
                join category in db.Categories
                on image.CategoryId equals category.CategoryId
                where category.CategoryName == cname
                select new ImageDetails
    {
        ImageId = image.ImageId,
        ImageName = image.ImageName,
        Category = category.CategoryName,
        Photographer = login.Name,
        Contact = login.Contact,
        ImageLink = image.ImageLink
    }).ToListAsync();

            return imageDetails.Count == 0 ? null : imageDetails;
            //var logins = await db.Logins.ToListAsync();
            //var images = db.Images.Include(p => p.Category);//lista kuvista

            //if (images.Count() == 0)
            //{
            //    return null; 
            //}
            //var categorysimages = images.Where(c => c.Category.CategoryName.Equals(cname)).ToList();//lista tietyn kategorian kuvista

            //if (categorysimages.IsNullOrEmpty()) 
            //{
            //    return null;
            //}

            //var imagedetails = categorysimages.Select(image => new ImageDetails //Luodaan kuvadetails luokan mukainen olio, joka palauttaa FrontEndiä varten muokatun datan.
            //{
            //    ImageId = image.ImageId,
            //    ImageName = image.ImageName,
            //    Category = image.Category?.CategoryName,
            //    Photographer = logins.FirstOrDefault(k => k.LoginId == image.LoginId)?.Name,
            //    Contact = logins.FirstOrDefault(k => k.LoginId == image.LoginId)?.Contact,
            //    ImageLink = image.ImageLink
            //}).ToList();


            //return imagedetails;
        }

        public async Task<List<ImageDetails>?> GetImagesByUserAsync(string email)
        {
            var user = await db.Logins.FirstOrDefaultAsync(x => x.Email == email); //Etsitään käyttäjä sähköpostin(unique määritys tietokannassa)perusteella
            
            if (user == null)
            { 
                return null; 
            }

            var images = await db.Images.Include(p => p.Category).ToListAsync();//lista kuvista
            var ownimages = images.Where(c => c.LoginId == user.LoginId).ToList();//lista kuvista käyttäjän perusteella

            if (images == null)
            {
                return null;
            }

            var imagedetails = ownimages.Select(image => new ImageDetails //Luodaan kuvadetails luokan mukainen olio, joka palauttaa FrontEndiä varten muokatun datan.
            {
                ImageId = image.ImageId,
                ImageName = image.ImageName,
                Category = image.Category?.CategoryName,
                Photographer = user.Name,
                Contact = user.Contact,
                ImageLink = image.ImageLink
            }).ToList();

            return(imagedetails);
        }

        public async Task<Image?> GetImageByIdAsync(int id)
        {
            return await db.Images.FirstOrDefaultAsync(i => i.ImageId == id);
        }

        public async Task<Image?> GetImageByNameAsync(string imagename)
        {
            Image? image = await db.Images.FirstOrDefaultAsync(i => i.ImageName == imagename);
            return image;
        }

        public async Task<(string Message, Byte[] ?Image)> LoadImageAsync(int id)
        {
            var image = await db.Images.FindAsync(id);//etsii kuvaolion id perusteella

            if (image == null)
            {   
                return("NotFound", null);
            }

            var ByteFile = image.ImageBytes;

            if (ByteFile == null)
            {
                return("NotFound", null);
            }
            var FileName = image.ImageName + ".jpg" ?? "ladattu_kuva.jpg";

            return (FileName, ByteFile);
        }

        public async Task<(bool Success, string Message, Image ?Image)> AddNewImageAsync(
        int loginId,
        string categoryName,
        string imageName,
        IFormFile ?imageBytes)
        {
            var allowedFileTypes = new List<string> { "image/jpeg", "image/png", "image/jpg", "image/webp" };
            var imageCount = await db.Images.CountAsync();

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

                    var category = await db.Categories.FirstOrDefaultAsync(c => c.CategoryName.Equals(categoryName));
                    var categoryId = category?.CategoryId;

                    var newImage = new Image
                    {
                        ImageName = imageName.Trim().ToLower(),
                        ImageLink = $"/kuva/{imageName}",
                        ImageBytes = binaryFile,
                        CategoryId = categoryId,
                        LoginId = loginId,
                    };

                    db.Images.Add(newImage);
                    await db.SaveChangesAsync();

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
