using OpiskelijanKuvapankki2_0.Controllers;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using System;

namespace OpiskelijanKuvapankki2_0.Services
{
    public class CleanUpService: ICleanUpService
    {
        private readonly OpiskelijanKuvapankki2_0Context _db;
        private readonly ILogger<CleanUpService> _logger;
        public CleanUpService(OpiskelijanKuvapankki2_0Context db, ILogger<CleanUpService> logger)
        {
            _db = db;
            _logger = logger;
        }

            public async Task UserCleanUpRoutine()
        {
            _logger.LogInformation("Vahvistamattomien käyttäjien siivousrutiini alkoi" + DateTime.Now.ToString());
           

            var cutoff = DateTime.UtcNow.AddMinutes(-1);

            var expiredIds = _db.EmailVerifications
                .Where(c => c.CreatedAt < cutoff)
                .Select(c => c.LoginID)
                .Distinct()
                .ToList();

            var expiredLogins = _db.Logins
            .Where(u => expiredIds.Contains(u.LoginId))
            .ToList();


            _db.Logins.RemoveRange(expiredLogins);
        await _db.SaveChangesAsync();
            
            _logger.LogInformation("Vahvistamattomien käyttäjien siivousrutiini loppui" + DateTime.Now.ToString());
        }
    
    }
}
