namespace OpiskelijanKuvapankki2_0.Dtos.ImageDtos
{
    
        public class UploadImageDto
        {
            public string CategoryName { get; set; }
            public string ImageName { get; set; }
            public IFormFile ImageFile { get; set; }
        }
    
}
