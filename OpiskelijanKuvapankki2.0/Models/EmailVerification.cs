namespace OpiskelijanKuvapankki2_0.Models
{
    public class EmailVerification
    {
        public string Email { get; set; }
        public string Code { get; set; }
        public DateTime TimeValid { get; set; }
    }
}
