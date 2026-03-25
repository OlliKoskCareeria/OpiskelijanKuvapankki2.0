using Microsoft.AspNetCore.Components.Forms;
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

           
            var validation = await userservice.ValidateUser(newuser);

            if (validation.Success == false)
            {
                return BadRequest(new { message = validation.Message });
            }

            Login validuser = await userservice.CreateUserAsync(newuser);

            

            bool verifyok = await userservice.SendVerifiCode(validuser);

            if (verifyok != true)
            {
                return Ok(new { message = "Tunnus luotu, mutta vahvistusviestin lähetys epäonnistui. Voit pyytää uuden viestin myöhemmin." });
            }

            return Ok(new { message = "Jos käyttäjä hyväksyttiin, vahvistusviesti on lähetetty." });

        }


        [HttpPost("verify")]
        public async Task<IActionResult> Verify (int loginid, string code)
        {
            
            var result = await userservice.VerifyCode(loginid,code);

            if (result.Success == false)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = "Vahvistus onnistui" });

        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUserAndImages(int id)  //Poistaa käyttäjän ja siihen liitetyt kuvat
        {
            VerificationResult result = await userservice.DeleteUserAndImagesAsync(id);

            if (result.Success == true)
            {
                return Ok(result);
            }
            else
            {
                if (result.Message != null&&result.Message.Contains("NotFound"))
                {
                    return NotFound(result);
                }
                else
                {
                    return StatusCode(500, result);
                }
            }
        }
        [HttpDelete("{id}/assign-images")]
        public async Task<ActionResult> DeleteUserSaveImages(int id)
        {
          var result = await userservice.DeleteUserAssignImages(id);

            return Ok(result.Success + result.Message);

        }

    }
}

