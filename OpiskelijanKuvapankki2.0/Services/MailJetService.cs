
using Microsoft.Extensions.Options;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Runtime;

namespace OpiskelijanKuvapankki2_0.Services
{
    public class MailJetService : IEmailService
    {
        private readonly MailJetSettings _settings;

        public MailJetService(IOptions<MailJetSettings> settings)
        {
            _settings = settings.Value;
        }
        

        

        
        // To send an email
        //MailMessage message = new MailMessage("from@example.com", "to@example.com", "Subject", "Body of the email");
        //client.Send(message);

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            SmtpClient client = new SmtpClient("in-v3.mailjet.com", 587)
            {
                Credentials = new NetworkCredential(_settings.ApiKey, _settings.SecretKey),
                EnableSsl = true
            };



            var from = new MailAddress(_settings.FromEmail);
            var to = new MailAddress(toEmail);
            var message = new MailMessage
            {
                From = from,
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
                 
            };
            message.To.Add(to);
            try
            {
               await client.SendMailAsync(message);
            }
            catch (SmtpException ex)
            {
                var mes = (ex, "SMTP failed: {Message}", ex.Message);
                
                throw new InvalidOperationException(mes.ToString());
            }
        }

    }
}








