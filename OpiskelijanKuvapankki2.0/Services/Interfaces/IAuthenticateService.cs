using Microsoft.AspNetCore.Identity.Data;
using OpiskelijanKuvapankki2_0.Dtos.AuthDtos;
using OpiskelijanKuvapankki2_0.Responses;
namespace OpiskelijanKuvapankki2_0.Services.Interfaces
{
    public interface IAuthenticateService
    {
        AuthResponse ?Authenticate(string KayttajaTunnus, string Ssana);

        Task<OperationResult> ResetPasswordAsync(ResetPasswordRequest request);
    }
}
