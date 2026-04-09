using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpiskelijanKuvapankki2_0.Dtos.LoginDtos;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Responses;
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

        [EnableRateLimiting("AddNewPolicy")]
        [HttpPost]
        public async Task<ActionResult> AddNew([FromBody] NewLoginDto newuser)
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
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("request-email-change")]
        public async Task<IActionResult>RequestEmailChange([FromBody]UpdateEmailDto updatemail)
        {

            var result = await userservice.ChangeEmail(updatemail);
            if(result == true)
            {
                return Ok("Vahvistusviesti lähetetty");
            }
            else
            {
                return BadRequest("Sähköpostin vaihtaminen epäonnistui. Tarkista, että sähköpostiosoite on kirjoitettu oikein (esim. nimi@esimerkki.com). Jos osoite on oikein, palvelimessamme saattaa olla hetkellinen häiriö. Yritä myöhemmin uudelleen.");
            }
        }
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("confirm-email-change")]
        public async Task<IActionResult> ConfirmEmailChange(string code, int loginId)
        {
            var result = await userservice.ConfirmEmailChangeAsync(code, loginId);

            if (result.Success == false)
            {
                return BadRequest(new { message = result.Message });
            }
            return Ok(new {message = result.Message});
        }
        [HttpPost("edit")]
        public async Task<IActionResult> Edit([FromForm] EditLoginDto edituser) //Rutiini nimi ja yhteystietojen muokkaamista varten
        {
            bool success = await userservice.EditUser(edituser);
            if (success == true)
            {
                return Ok(new { message = "Käyttäjätietojen muokkaus onnistui" });
            }
            else return BadRequest(new { message = "Käyttäjätietojen muokkaus epäonnistui" });    
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
            OperationResult result = await userservice.DeleteUserAndImagesAsync(id);

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

