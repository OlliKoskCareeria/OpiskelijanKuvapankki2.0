namespace OpiskelijanKuvapankki2_0.Dtos.ImageDtos
{
    public class ImageDetails //tämä luokka vastaa frontendissä näytettäviä tietoja
    {
        public required int ImageId { get; set; }

        public string? ImageName { get; set; }

        public string? Photographer { get; set; }

        public string? Contact { get; set; }

        public string? Category { get; set; }

        public string? ImageLink { get; set; }
    }
}
