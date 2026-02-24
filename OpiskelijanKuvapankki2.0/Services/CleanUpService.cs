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

            public async Task DailyCleanUpRoutine()
        {
            _logger.LogInformation("Päivittäinen siivousrutiini alkoi" + DateTime.Now.ToString());
          
            await UserCleanUpRoutine();
            await ExpiredResetsCleanUpRoutine();

            _logger.LogInformation("Päivittäinen siivousrutiini loppui" + DateTime.Now.ToString());
        }

        private async Task UserCleanUpRoutine()
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddHours(-24);

                var expiredIds = _db.EmailVerifications
                    .Where(c => c.CreatedAt < cutoff)
                    .Select(c => c.LoginID)
                    .Distinct()
                    .ToList();

                var expiredLogins = _db.Logins
                .Where(u => expiredIds.Contains(u.LoginId))
                .ToList();


                _db.Logins.RemoveRange(expiredLogins);
                var deleted = await _db.SaveChangesAsync();
                _logger.LogInformation("Poistettiin {count} vahvistamatonta käyttäjää.", deleted);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex,"Käyttäjien siivousrutiini ei siivonnut");
            }
        }

        private async Task ExpiredResetsCleanUpRoutine()
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddHours(-24);

                var unvalidResets = _db.PasswordResets
                    .Where(p => p.CreatedAt < cutoff)
                    .ToList();

                _db.PasswordResets.RemoveRange(unvalidResets);
                var deleted = await _db.SaveChangesAsync();

                _logger.LogInformation("Poistettiin {count} ei validia salasanan resetointi objektia.", deleted);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex,"ExpiredResets siivourutiini ei siivonnut " + DateTime.Now.ToString());
            }
        }

    }
}
