namespace OpiskelijanKuvapankki2_0.Dtos.LoginDtos
{
    public class UpdateEmailDto
    {
        public int LoginId { get; set; }

        public string NewEmail { get; set; } = null!;
    }
}
