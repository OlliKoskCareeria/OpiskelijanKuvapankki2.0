namespace OpiskelijanKuvapankki2_0.Dtos.ImageDtos
{
    
        public class UploadImageDto
        {
            public required string CategoryName { get; set; }
            public required string ImageName { get; set; }
            public IFormFile ?ImageFile { get; set; }
        }
    
}
