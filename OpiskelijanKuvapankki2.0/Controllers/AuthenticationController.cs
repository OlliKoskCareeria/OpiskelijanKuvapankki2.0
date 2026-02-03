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
            AuthResponse loggedUser = _authenticateService.Authenticate(tunnukse.UserName, tunnukse.PassWord);

            if (loggedUser == null)
                return BadRequest(new { message = "Sisäänkirjautuminen epäonnistui" });

            return Ok(loggedUser); // Palauttaa AuthResponse olion sis LoggedUser ja jwt Token
        }
        

    }
}
