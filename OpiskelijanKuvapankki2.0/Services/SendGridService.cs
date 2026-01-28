using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Options;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using OpiskelijanKuvapankki2_0.Models;

namespace OpiskelijanKuvapankki2_0.Services
{
    public class SendGridService : IEmailService
    {
        private readonly SendGridSettings _settings;

        public SendGridService(IOptions<SendGridSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var client = new SendGridClient(_settings.ApiKey);
            var from = new EmailAddress(_settings.FromEmail, _settings.FromEmail);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);

            var response = await client.SendEmailAsync(msg);

            if(!response.IsSuccessStatusCode)
            {
                var body = await response.Body.ReadAsStringAsync();
                throw new InvalidOperationException($"SendGrid send failed:{response.StatusCode} -{body}");
            }
        }
    }
}
