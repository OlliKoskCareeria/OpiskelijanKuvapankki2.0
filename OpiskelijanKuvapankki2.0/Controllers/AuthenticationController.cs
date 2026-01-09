using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using System.Reflection.Metadata.Ecma335;

namespace OpiskelijanKuvapankki2_0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private IAuthenticateService _authenticateService;

        public AuthenticationController(IAuthenticateService authenticateService)
        {
            _authenticateService = authenticateService;
        }

        // Front endin kirjautumisyritys
        [HttpPost]
        public ActionResult Post([FromBody] Credentials tunnukse)
        {
            var loggedUser = _authenticateService.Authenticate(tunnukse.UserName, tunnukse.PassWord);

            if (loggedUser == null)
                return BadRequest(new { message = "Käyttäjätunnus tai salasana on virheellinen" });

            return Ok(loggedUser); // Palautus front endiin (sis. vain loggedUser luokan mukaiset kentät)
        }
        //private readonly UserService userservice = _userservice;
        //private readonly ILogger<AuthenticationController> logger = _logger;
        //private readonly OpiskelijanKuvapankki2_0Context db = _db;

        //[HttpPost("register")]
        //public async Task<IActionResult> Register(RegisterRequest request)
        //    => await userservice.RegisterAsync(request);


        //[HttpPost("verify")]
        //public async Task<IActionResult> VerifyEmailRequest request)
        //    => await userservice.VerifyEmailAsync(request);


    }
}
