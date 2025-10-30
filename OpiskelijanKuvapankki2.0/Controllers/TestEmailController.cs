using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using System.Security.Cryptography;

namespace OpiskelijanKuvapankki2_0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestEmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public TestEmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet("send")]
        public async Task<IActionResult> SendTestEmail()
        {
            var html = "<h1>Hello from SendGrid</h1><p>tämä on testi</p>";
            await _emailService.SendEmailAsync("olli.koski@opiskelijankuvapankki.fi", "Testi", html);

            return Ok("lähetys onnistui!");
        }
    }
}
