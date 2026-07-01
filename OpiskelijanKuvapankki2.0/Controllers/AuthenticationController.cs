using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpiskelijanKuvapankki2_0.Dtos.AuthDtos;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Responses;
using OpiskelijanKuvapankki2_0.Services;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace OpiskelijanKuvapankki2_0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private IAuthenticateService _authenticateService;
        private IRecaptchaService _recaptchaService;
        private UserService _userService;
        private OpiskelijanKuvapankki2_0Context _db;
        private IEmailService _emailService;
        public AuthenticationController
            (
            IAuthenticateService authenticateService,
            IRecaptchaService recaptchaService,
            UserService userService,
            OpiskelijanKuvapankki2_0Context db,
            IEmailService emailService
            )
        {
            _db = db;
            _authenticateService = authenticateService;
            _recaptchaService = recaptchaService;
            _userService = userService;
            _emailService = emailService;
        }

        // Front endin kirjautumisyritys
        [EnableRateLimiting("fixed")]
        [HttpPost]
        public ActionResult Post([FromBody] Credentials tunnukse)
        {
            AuthResponse ?loggedUser = _authenticateService.Authenticate(tunnukse.UserName, tunnukse.PassWord);

            if (loggedUser == null)
                return BadRequest(new { message = "Sisäänkirjautuminen epäonnistui" });

            return Ok(loggedUser); // Palauttaa AuthResponse olion sis LoggedUser ja jwt Token
        }
        [EnableRateLimiting("FixedForIp")]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            
            var user =  _db.Logins.FirstOrDefault(u => u.Email == request.Email);

            if (user != null)
            {
                PasswordReset? entity = await _userService.ForgotPasswordSendCode(user);

                if (entity != null)
                {
                    entity.CreatedByIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _db.PasswordResets.AddAsync(entity);
                    await _db.SaveChangesAsync();
                }
            }

            return Ok(new { message = "Jos sähköposti löytyy. Ohjeet lähetetty." });
        }
        [EnableRateLimiting("ResetPasswordPolicy")]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            OperationResult result = await _authenticateService.ResetPasswordAsync(request);

            if(result.Success == false)
            {
                return BadRequest(result.Message);            
            }
           
            return Ok("reset Ok");
        }


    }
}
