using OpiskelijanKuvapankki2_0.Models;
namespace OpiskelijanKuvapankki2_0.Services.Interfaces
{
    public interface IAuthenticateService
    {
        LoggedUser Authenticate(string KayttajaTunnus, string Ssana);
    }
}
