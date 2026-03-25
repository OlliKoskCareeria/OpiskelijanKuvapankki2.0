
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using OpiskelijanKuvapankki2_0.Controllers;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net.Mail;
using System.Reflection.Metadata.Ecma335;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        public async Task<VerificationResult> VerifyCode(int loginid, string code)
        {
            VerificationResult result = new VerificationResult();

            var record = db.EmailVerifications.FirstOrDefault(v => v.LoginID ==loginid);

            if (record == null) 
            {
                result.Message = "Käyttäjää ei löydy";
                result.Success = false;
                return result; 
            }
            if (record.TimeValid <= DateTime.Now||record.Attempts > 4)
            {
                result.Message = "Koodi ei ole voimassa";
                result.Success = false;
                return result;
            }

            var checkcode = VerifyPassword(record.Code, code);

            if(checkcode == false)
            {
                result.Message = "Virheellinen koodi";
                result.Success = false;
                record.Attempts++;
                await db.SaveChangesAsync();
                return result;
            }
            
            
            var user = db.Logins.FirstOrDefault(v => v.LoginId == loginid);

            if (user == null) {
                result.Message = "Tapahtui odottamaton virhe";
                result.Success = false;
            }

            result.Success = true;
            result.Message = "Sähköposti vahvistettu";
            user.Status = "VERIFIED";
            db.EmailVerifications.Remove(record);
            await db.SaveChangesAsync();
                await emailService.SendEmailAsync(user.Email, "Tervetuloa",result.Message);
                return result;
            
        }

            
        public async Task<VerificationResult> DeleteUserAssignImages(int id)
        {
            VerificationResult result = new VerificationResult();

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
            catch (Exception ex) { 
                result.Success = false;
                result.Message = ex.Message;
                return (result);
            }
        }

        public async Task<VerificationResult> DeleteUserAndImagesAsync(int id)
        {
            VerificationResult result = new VerificationResult();

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
                await emailService.SendEmailAsync(user.Email,"palauta salasanasi", message);
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


    }
}
