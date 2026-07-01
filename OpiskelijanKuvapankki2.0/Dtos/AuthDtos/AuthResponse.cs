namespace OpiskelijanKuvapankki2_0.Dtos.AuthDtos
{
    public class AuthResponse
    {
        public LoggedUser? loggedUser { get; set; }
        public string? Token { get; set; }
    }
}
