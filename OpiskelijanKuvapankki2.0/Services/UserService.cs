
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using OpiskelijanKuvapankki2_0.Controllers;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Reflection.Metadata.Ecma335;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OpiskelijanKuvapankki2_0.Services
{
    public class UserService(OpiskelijanKuvapankki2_0Context _db, ILogger<LoginsController> _logger, IEmailService _emailService, IConfiguration _getdetails)
    {
        private readonly IConfiguration getdetails = _getdetails;
        private readonly OpiskelijanKuvapankki2_0Context db = _db;
        private readonly ILogger<LoginsController> logger = _logger;
        private List<string> domains;
        
        private readonly IEmailService emailService = _emailService;

        public string UserValidation(Login newuser)
        {
            

            var organisations = db.Organisations.ToList();
            domains = organisations.Select(x => x.Domain).ToList();

            try
            {
                var existingLogin = db.Logins.FirstOrDefault(l => l.Email == newuser.Email);//tarkistetaan onko käyttäjätunnus käytössä
                string newUserDomain = newuser.Email.Split('@')[1];
                bool emailSyntaxCheck = newuser.Email.Count(c => c == '@') < 2;//tarkistetaan onko käyttäjätunnus sähköposti

                if (emailSyntaxCheck == false)
                {

                    return ("Käyttäjätunnuksen syntaxi on virheellinen");
                }
                if (existingLogin != null)
                {

                    return ("Tunnuksen luonti ei onnistunut");//Käyttäjätunnus varattu
                }
                if (domains.Contains(newUserDomain, StringComparer.OrdinalIgnoreCase) || newuser.Email == AdminUser())//tarkistetaan että sähköposti on hyväksytty
                {
                    var emailformessage = newuser.Email;
                    newuser.Pword = HashPassword(newuser.Pword);



                    
                        db.Logins.Add(newuser);
                        db.SaveChanges();
                        logger.LogInformation("Uusi käyttäjä {emailformessage}", emailformessage);
                        return ("lisättiin uusi käyttäjä " + newuser.Email );
                    
                }
                else
                {

                    return ("Tunnusta ei voi luoda tälle sähköpostiosoitteelle");
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Virhe sisäänkirjautumisessa {DateTime.Now}");
                return ("Tapahtui virhe. Lue lisää: " + e.InnerException);
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

        

        private readonly PasswordHasher<object> _hasher = new();

        public string HashPassword(string password)
            => _hasher.HashPassword(null, password);

        public bool VerifyPassword(string hash, string password)

            => _hasher.VerifyHashedPassword(null, hash, password)
               != PasswordVerificationResult.Failed;


    }
}
