namespace OpiskelijanKuvapankki2_0.Dtos.LoginDtos
{
    public class NewLoginDto
    {
        public string Email { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? Contact { get; set; }

        public string Pword { get; set; } = null!;
    }
}
