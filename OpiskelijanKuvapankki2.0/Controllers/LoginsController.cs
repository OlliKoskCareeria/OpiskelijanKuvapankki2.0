using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services;
using Microsoft.Extensions.Logging;


namespace OpiskelijanKuvapankki2_0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginsController(OpiskelijanKuvapankki2_0Context _db, UserService _userservice, ILogger<LoginsController> _logger) : ControllerBase
    {
        private readonly OpiskelijanKuvapankki2_0Context db = _db;
        private readonly UserService userservice = _userservice;
        private readonly ILogger<LoginsController> logger = _logger;


        [HttpPost]
        public ActionResult AddNew([FromBody] Login newuser)
        {
            try
            {
                var existingLogin = db.Logins.FirstOrDefault(l => l.Email == newuser.Email);//tarkistetaan onko käyttäjätunnus käytössä
                if (existingLogin != null)
                {
                    logger.LogInformation("käyttäjä varattu");
                    return BadRequest(new { Message = "Käyttäjätunnus on varattu" });
                }
                if (newuser.Email.EndsWith(userservice.BasicUser()) || newuser.Email == userservice.AdminUser())//tarkistetaan että sähköposti on hyväksytty
                {
                    var emailformessage = newuser.Email;
                    db.Logins.Add(newuser);
                    db.SaveChanges();
                    logger.LogInformation("Uusi käyttäjä {emailformessage}", emailformessage);
                    return Ok($"Lisättiin uusi Kayttaja {newuser.Email}");
                }
                else
                {
                    logger.LogInformation("Sähköposti ei sallittu");
                    return BadRequest(new { Message = "Tunnusta ei voi luoda tälle sähköpostiosoitteelle" });
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Virhe sisäänkirjautumisessa {DateTime.Now}");
                return BadRequest("Tapahtui virhe. Lue lisää: " + e.InnerException);
            }
        }
    }
}

