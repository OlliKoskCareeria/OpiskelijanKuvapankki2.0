namespace OpiskelijanKuvapankki2_0.Services.Interfaces
{
    public interface IRecaptchaService
    {
        
        Task<bool> VerifyAsync(string token, string expectedAction);
        

    }
}
