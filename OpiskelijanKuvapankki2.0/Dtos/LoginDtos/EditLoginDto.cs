namespace OpiskelijanKuvapankki2_0.Dtos.LoginDtos
{
    public class EditLoginDto
    {
        public int LoginId { get; set; }

        public string Name { get; set; } = null!;

        public string? Contact { get; set; }
    }
}
