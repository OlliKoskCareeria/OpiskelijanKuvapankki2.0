using OpiskelijanKuvapankki2_0.Configurations;
using OpiskelijanKuvapankki2_0.Controllers;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using System.Globalization;
using System.Text.Json;
namespace OpiskelijanKuvapankki2_0.Services
{
    public class RecaptchaService : IRecaptchaService

    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public RecaptchaService(IConfiguration configuration, HttpClient httpClient, ILogger<RecaptchaService> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<bool> VerifyAsync(string token, string expectedAction)
        {
            var secret = _configuration["Recaptcha:SecretKey"];
            //var minScore = float.Parse(_configuration["Recaptcha:MinimumScore"]);
            var minScore = float.Parse(
                _configuration["Recaptcha:MinimumScore"]!,
                CultureInfo.InvariantCulture
                );
            
            var response = await _httpClient.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={token}",
                null);

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RecaptchaResponse>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

            if (result == null)
            {
                _logger.LogWarning("reCAPTCHA verification failed: Google returned an invalid response.");
                return false;
            }
            return result.Success
            && result.Score >= minScore
            && result.Action == expectedAction;
        }

        //public async Task<bool> VerifyRecaptchaAsync(string token)
        //{
        //    var secret = _configuration["Recaptcha:SecretKey"];
        //    using var http = new HttpClient();

        //    var response = await http.PostAsync(
        //        $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={token}",
        //        null);

        //    var json = await response.Content.ReadAsStringAsync();
        //    var result = JsonSerializer.Deserialize<RecaptchaResponse>(json);

        //    return result.Success;
        //}

        

    }


}
