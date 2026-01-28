using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using System.Security.Cryptography;
using Microsoft.AspNetCore.RateLimiting;
namespace OpiskelijanKuvapankki2_0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet("send")]
        public async Task<IActionResult> SendTestEmail()
        {
            var html = "<h1>Hello world</h1><p>Hello world</p>";
            await _emailService.SendEmailAsync("ollikoski84@gmail.com", "Testi", html);

            return Ok("lähetys onnistui!");
        }
        [EnableRateLimiting("fixed")]
        [HttpPost("testaa")]
        public IActionResult Testaa([FromBody]string email)
        {
            return Ok("testiok");
        }

        [EnableRateLimiting("FixedForIp")]
        [HttpPost("IpTesti")]
        public IActionResult IpTesti([FromBody] string email)
        {
            return Ok("Iptestiok");
        }
    }
}
