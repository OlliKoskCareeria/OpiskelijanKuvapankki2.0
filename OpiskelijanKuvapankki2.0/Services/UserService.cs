using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpiskelijanKuvapankki2_0.Controllers;
using OpiskelijanKuvapankki2_0.Dtos.LoginDtos;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Responses;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using SixLabors.ImageSharp;
using System.Net.Mail;
using System.Security.Claims;


namespace OpiskelijanKuvapankki2_0.Services
{
    public class UserService(OpiskelijanKuvapankki2_0Context _db, ILogger<LoginsController> _logger, IEmailService _emailService, IConfiguration _getdetails)
    {
        private readonly IConfiguration getdetails = _getdetails;
        private readonly OpiskelijanKuvapankki2_0Context db = _db;
        private readonly ILogger<LoginsController> logger = _logger;
        private List<string>? domains;

        private readonly IEmailService emailService = _emailService;

        public async Task<Login> CreateUserAsync(Login newuser)
        {
            newuser.Pword = HashPassword(newuser.Pword);

            await db.Logins.AddAsync(newuser);
            await db.SaveChangesAsync();

            logger.LogInformation("Uusi käyttäjä {email}", newuser.Email);

            return newuser;
        }


        public async Task<(bool Success, string Message)> ValidateUser(Login newuser)
        {


            var domains = await db.Organisations.Select(x => x.Domain).ToListAsync();

            var existingLogin = await db.Logins.FirstOrDefaultAsync(l => l.Email == newuser.Email);

            if (existingLogin != null) //Tarkistetaan onko sähköposti käytössä
            {
                return (false, "Tunnuksen luonti epäonnistui");
            }

            try
            {
                var addr = new MailAddress(newuser.Email);    //Tarkistetaan sähköpostin oikea muoto
            }
            catch
            {
                return (false, "Virheellinen sähköposti");
            }

            string newUserDomain = newuser.Email.Split('@')[1];

            if (domains.Contains(newUserDomain, StringComparer.OrdinalIgnoreCase)
                || string.Equals(newuser.Email, AdminUser(), StringComparison.OrdinalIgnoreCase)) //Tarkistetaan onko sähköposti hyväksytty
            {

                return (true, "Tarkistus OK");
            }
            else
            {

                return (false, "Tunnuksen luonti epäonnistui");
            }
        }
        public string AdminUser()
        {

            string AdminUserEmail = getdetails["AdminUser:Email"];
            return AdminUserEmail;
        }



        private static string GenerateCode(int length = 6)
        {
            var random = new Random();
            var code = string.Join("", Enumerable.Range(0, length)
                .Select(_ => random.Next(0, 10)));
            return code;
        }

        public async Task<bool> SendVerifiCode(Login newuser)
        {
            EmailVerification emailVerification = new EmailVerification();
            var code = GenerateCode();
            emailVerification.LoginID = newuser.LoginId;
            emailVerification.Code = _hasher.HashPassword(newuser, code);
            emailVerification.Attempts = 0;
            emailVerification.CreatedAt = DateTime.Now;
            emailVerification.TimeValid = DateTime.Now.AddMinutes(30);

            db.EmailVerifications.Add(emailVerification);
            db.SaveChanges();

            var message = $"Vahvista sähköpostisi. Tämä koodi on voimassa 30minuuttia:{code}";
            try
            {
                await emailService.SendEmailAsync(newuser.Email, "Vahvista sähköpostisi", message);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(message: ex.Message);
                return false;
            }
        }

        public async Task<OperationResult> VerifyCode(int loginid, string code)
        {
            OperationResult result = new OperationResult();

            var record = db.EmailVerifications.FirstOrDefault(v => v.LoginID == loginid);

            if (record == null)
            {
                result.Message = "Käyttäjää ei löydy";
                result.Success = false;
                return result;
            }
            if (record.TimeValid <= DateTime.Now || record.Attempts > 4)
            {
                result.Message = "Koodi ei ole voimassa";
                result.Success = false;
                return result;
            }

            var checkcode = VerifyPassword(record.Code, code);

            if (checkcode == false)
            {
                result.Message = "Virheellinen koodi";
                result.Success = false;
                record.Attempts++;
                await db.SaveChangesAsync();
                return result;
            }


            var user = db.Logins.FirstOrDefault(v => v.LoginId == loginid);

            if (user == null)
            {
                result.Message = "Tapahtui odottamaton virhe";
                result.Success = false;
            }

            result.Success = true;
            result.Message = "Sähköposti vahvistettu";
            user.Status = "VERIFIED";
            db.EmailVerifications.Remove(record);
            await db.SaveChangesAsync();
            await emailService.SendEmailAsync(user.Email, "Tervetuloa", result.Message);
            return result;

        }


        public async Task<OperationResult> DeleteUserAssignImages(int id)
        {
            OperationResult result = new OperationResult();

            try
            {
                var getarchive = getdetails["AdminUser:Archive"];

                var user = db.Logins.FirstOrDefault(v => v.LoginId == id);

                if (user == null)
                {
                    result.Message = "käyttäjää ei löytynyt";
                    result.Success = false;
                    return result;
                }

                var archive = db.Logins.FirstOrDefault(a => a.Email.Equals(getarchive));

                if (archive == null)
                {
                    result.Message = "Arkisto ei ole saatavilla";
                    result.Success = false;
                    return result;
                }

                var userimages = db.Images.Where(c => c.LoginId == user.LoginId);

                var message = " Käyttäjätili poistettu ja käyttäjätiliin liittyvät kuvat siirretty ylläpitoon.";

                if (userimages.Count() > 0)
                {
                    foreach (var img in userimages)
                    {
                        img.LoginId = archive.LoginId;

                    }
                    db.Images.UpdateRange(userimages);


                }

                db.Logins.Remove(user);
                await db.SaveChangesAsync();
                result.Success = true;
                result.Message = message;
                return (result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                return (result);
            }
        }

        public async Task<OperationResult> DeleteUserAndImagesAsync(int id)
        {
            OperationResult result = new OperationResult();

            try
            {

                var login = db.Logins.Find(id);

                if (login != null)
                {
                    var userimages = db.Images.Where(c => c.LoginId == login.LoginId);

                    db.Images.RemoveRange(userimages);

                    db.Logins.Remove(login);

                    db.SaveChanges();
                    result.Success = true;
                    result.Message = login.Email + "poistettiin onnistuneesti";

                    return result;
                }
                result.Success = false;
                result.Message = "NotFound";
                return result;
            }
            catch
            {
                result.Success = false;
                result.Message = "Käyttäjän poistossa tapahtui virhe. Yritä uudelleen myöhemmin.";
                return result;
            }
        }

        public async Task<PasswordReset?> ForgotPasswordSendCode(Login user)
        {
            var now = DateTime.UtcNow;



            var oldcode = await db.PasswordResets.FirstOrDefaultAsync(c => c.UserId == user.LoginId);

            if (oldcode != null && oldcode.ExpiresAt > now)
            {
                logger.LogInformation("Käyttäjällä on aktiivinen PasswordReset objekti {UserId}", user.LoginId);

                return null; // Voimassa oleva koodi löytyy uutta ei luoda
            }

            if (oldcode != null)//Poistaa vanhentuneen PasswordReset olion mikäli sellainen löytyy
            {
                db.PasswordResets.Remove(oldcode);
                await db.SaveChangesAsync();
            }

            var code = GenerateCode();
            var hash = HashPassword(code);

            PasswordReset entity = new()
            {

                UserId = user.LoginId,
                CodeHash = hash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                CreatedByIp = null

            };



            var message = $"Palauta salasanasi. Tämä koodi on voimassa 15minuuttia:{code}";
            try
            {
                await emailService.SendEmailAsync(user.Email, "palauta salasanasi", message);
                return entity;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send password reset email for user {UserId}", user.LoginId);
                return null;
            }
        }

        private readonly PasswordHasher<object> _hasher = new();

        public string HashPassword(string password)
            => _hasher.HashPassword(null, password);

        public bool VerifyPassword(string hash, string password)

            => _hasher.VerifyHashedPassword(null, hash, password)
               != PasswordVerificationResult.Failed;

        public async Task<bool> EditUser(EditLoginDto dto)
        {
            try
            {
                var login = await db.Logins.FindAsync(dto.LoginId);

                if (login == null)
                    return false;


                login.Contact = dto.Contact;
                login.Name = dto.Name;

                await db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ChangeEmail(UpdateEmailDto update)
        {
            var checkmail = await db.Logins.FirstOrDefaultAsync(x => x.Email == update.NewEmail);

            if (checkmail != null) return false; //tarkistetaan onko kyseinen sähköposti käytössä
            var checkuser = await db.Logins.FirstOrDefaultAsync(c => c.LoginId == update.LoginId);
            //var userId = GetUserIdFromClaims();
            if (checkuser == null) return false; //Frontendistä tulevasta kirjautuneen käyttäjän pyynnöstä pitäisi käytännössä löytyä validi id, mutta tarkistetaan silti löytyykö käyttäjä
            try
            {
                var checksyntax = new MailAddress(update.NewEmail);  //Tarkistetaan sähköpostin muoto
            }
            catch
            {
                return (false);
            }

            var verification = await RequestEmailChange(update); //luodaan vahvistus objekti ja lähetetään koodi
            if (verification.Success == true)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public async Task<OperationResult> RequestEmailChange(UpdateEmailDto update)
        {
    //        var loginId = int.Parse(
    //    User.FindFirst(ClaimTypes.NameIdentifier).Value
    //);

            var oldverification = await db.EmailChangeRequests.FirstOrDefaultAsync(x => x.LoginId == update.LoginId);
            if (oldverification != null)
            {
                db.EmailChangeRequests.Remove(oldverification);
                await db.SaveChangesAsync();
            }
            OperationResult result = new OperationResult();
            EmailChangeRequest request = new EmailChangeRequest();
            var code = GenerateCode();
            request.LoginId = update.LoginId;
            request.NewEmail = update.NewEmail;
            request.Code = HashPassword(code);
            request.Attempts = 0;
            request.CreatedAt = DateTime.UtcNow;
            request.TimeValid = DateTime.UtcNow.AddMinutes(30);

            var message = $"Vahvista uusi sähköpostiosoite. Tämä koodi on voimassa 30minuuttia:{code}";
            try
            {
                await db.EmailChangeRequests.AddAsync(request);
                await db.SaveChangesAsync();

                await emailService.SendEmailAsync(update.NewEmail, "Vahvista uusi sähköpostiosoite", message);

                result.Success = true;
                result.Message = "Vahvistuskoodi lähetetty";
                return result;

            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Päivitys epäonnistui";
                logger.LogError
                    (ex, "RequestEmailChange. Pyyntö epäonnistui LoginId: {LoginId}, NewEmail: {NewEmail}",
                 update.LoginId,
                 update.NewEmail);
                return result;
            }


        }

        public async Task<OperationResult> ConfirmEmailChangeAsync(string code, int loginId)
        {
            OperationResult result = new OperationResult();

            //        var loginId = int.Parse(
            //    User.FindFirst(ClaimTypes.NameIdentifier).Value
            //);


            var changerequest = await db.EmailChangeRequests.FirstOrDefaultAsync(v => v.LoginId == loginId);

            if (changerequest == null)
            {
                result.Message = "Käyttäjää ei löydy";
                result.Success = false;
                return result;
            }
            if (changerequest.TimeValid <= DateTime.UtcNow || changerequest.Attempts > 4)
            {
                result.Message = "Koodi ei ole voimassa";
                result.Success = false;
                return result;
            }

            var checkcode = VerifyPassword(changerequest.Code, code);

            if (checkcode == false)
            {
                result.Message = "Virheellinen koodi";
                result.Success = false;
                changerequest.Attempts++;
                await db.SaveChangesAsync();
                return result;
            }
            
                    var user = await db.Logins.FirstOrDefaultAsync(c => c.LoginId == changerequest.LoginId);

                    if (user == null)
                   {
                        result.Success = false;
                        result.Message = "Käyttäjää ei löytynyt";
                        return result;
                    }
            result.Success = true;
            result.Message = "Sähköpostin päivitys onnistui";
            var oldmail = user.Email;
            user.Email = changerequest.NewEmail;
            db.EmailChangeRequests.Remove(changerequest);
            try
            {

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {

                logger.LogError(ex, "Tietokannan päivitys epäonnistui ConfirmEmailChangeAsync. LoginId: {LoginId}, NewEmail: {NewEmail}",
                changerequest.LoginId,
                changerequest.NewEmail);
                    
                result.Success = false;
                result.Message = "Sähköpostin päivitys epäonnistui";
                return result;
            }
            if (result.Success == true)
            {
                try
                {

                    await emailService.SendEmailAsync(oldmail, "Turvallisuus tiedote", "Tilisi sähköposti vaihdettiin. Jos, tämä et ollut sinä, ota yhteyttä tukipalveluumme");
                    await emailService.SendEmailAsync(user.Email, "Turvallisuus tiedote", "Tilisi sähköpostiosoite on päivitetty");
                    return result;
                }
                catch (Exception ex)
                {

                    logger.LogError(ex, "Vahvistusviestin lähetys epäonnistui ConfirmEmailChangeAsync LoginId: {LoginId}, NewEmail: {NewEmail}",
                    changerequest.LoginId,
                    changerequest.NewEmail);

                    result.Success = true;
                    result.Message = "Sähköpostin päivitys onnistui. Vahvistusviestiä ei lähetetty.";
                    return result;
                }
            }
            else
            {
                result.Success = false;
                result.Message = "Sähköpostin päivitys epäonnistui";
                return result;
            }
            
        }

    }
}
