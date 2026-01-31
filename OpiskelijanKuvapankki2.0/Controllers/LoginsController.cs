using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using System.Threading.Tasks;



namespace OpiskelijanKuvapankki2_0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginsController(OpiskelijanKuvapankki2_0Context _db, UserService _userservice, ILogger<LoginsController> _logger,IConfiguration _configuration, IRecaptchaService _recaptchaService) : ControllerBase
    {
        private readonly OpiskelijanKuvapankki2_0Context db = _db;
        private readonly UserService userservice = _userservice;
        private readonly ILogger<LoginsController> logger = _logger;
        private readonly IConfiguration configuration = _configuration;
        private readonly IRecaptchaService recaptchaservice = _recaptchaService;

        [HttpPost]
        public async Task<ActionResult> AddNew([FromBody] Login newuser)
        {
            if (configuration.GetValue<bool>("Recaptcha:Enabled"))
            {
                if (!Request.Headers.TryGetValue("X-Recaptcha-Token", out var token))
                {
                    return BadRequest(new { message = "Recaptcha token puuttuu." });
                }

                bool captchaValid = await recaptchaservice.VerifyAsync(
                    token!,
                    expectedAction: "Add_New_User"
                    
                );

                if (captchaValid != true)
                {
                    return BadRequest(new { message = "Recaptcha vahvistus epäonnistui." });
                }
            }

           
            string message = userservice.UserValidation(newuser);

            Login verifuser = db.Logins.FirstOrDefault(c => c.LoginId == newuser.LoginId);

            if(verifuser == null)
            {
                return Ok(new { message });
            }

            bool verifyok = await userservice.SendVerifiCode(verifuser);

            if (verifyok != true)
            {
                return BadRequest("Sähköpostivahvistuksen lähetys epäonnistui :( yritä myöhemmin uudestaan");
            }

            return Ok(new { message });

        }

        //[HttpPost("register")]
        
        //public async Task<IActionResult> Register([FromBody]int LoginId)
        //{
        //    Login login = db.Logins.FirstOrDefault(c => c.LoginId == LoginId);

        //    await userservice.SendVerifiCode(login);

        //    return Ok(new { message = "Vahvistuskoodi lähetetty" });

        //}

        [HttpPost("verify")]
        public async Task<IActionResult> Verify (int loginid, string code)
        {
            
            var result = await userservice.VerifyCode(loginid,code);
            if (result.Success == false)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = "Vahvistus onnistui" });

        }
    }
}

