using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;


namespace OpiskelijanKuvaPankki.Services

{
    public class AuthenticateService : IAuthenticateService
    {

        private readonly OpiskelijanKuvapankki2_0Context db;

        private readonly AppSettings _appSettings;

        private readonly UserService _userservice;

        private readonly IEmailService _emailservice;
        public AuthenticateService
            (
            IOptions<AppSettings> appSettings,
            OpiskelijanKuvapankki2_0Context okc,
            UserService userservice,
            IEmailService emailservice
            )
        {
            _appSettings = appSettings.Value;
            db = okc;
            _userservice = userservice;
            _emailservice = emailservice;
        }


        
        public AuthResponse Authenticate(string email, string pword)
        {

            var foundUser = db.Logins.SingleOrDefault(x => x.Email == email);

            if (foundUser == null||foundUser.Status.Equals("PENDING"))
                return null;

            var isValid = _userservice.VerifyPassword(foundUser.Pword, pword);

            if (!isValid)
                return null;

            

            // Jos käyttäjä löytyy:
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, foundUser.LoginId.ToString()),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim(ClaimTypes.Version, "V3.1")
                }),
                Expires = DateTime.UtcNow.AddDays(3),

                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            int accesslevelid = 0;


            if (foundUser != null && !string.IsNullOrEmpty(foundUser.Email))
            {



                if (foundUser.Email.Equals(_userservice.AdminUser(), StringComparison.OrdinalIgnoreCase))
                {
                    accesslevelid = 1;

                    //acceslevel käyttäjätunnuksen perusteella
                }
                else 
                {
                    accesslevelid = 2;


                }
                
            }
            else
            {

                Console.WriteLine($"foundUser tai KayttajaTunnus on null tai tyhjä, accesslevelid {accesslevelid}");
            }

            AuthResponse authResponse = new AuthResponse();

            LoggedUser loggedUser = new LoggedUser();

            loggedUser.UserName = foundUser.Email;
            loggedUser.LoginId = foundUser.LoginId;
            loggedUser.AccesslevelId = accesslevelid;
            loggedUser.Name = foundUser.Name;
            loggedUser.Contact = foundUser.Contact;

            authResponse.loggedUser = loggedUser;
            authResponse.Token = tokenHandler.WriteToken(token);
            return authResponse; // Palautetaan controllerimetodille


            
        }

        public async Task<OperationResult> ResetPasswordAsync(ResetPasswordRequest request)//lisää käsittely, jossa token poistetaan 5 yrityksen jälkeen ja rutiini joka päättää mitä tehdään jos sama sähköposti pyytää useita tokeneita
        {
            OperationResult result = new();

            var user = await db.Logins.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                result.Message = "Virheellinen pyyntö";
                result.Success = false;
                return result; //Sähköposti osoitetta/Käyttäjää ei löydy
            }

            var reset = await db.PasswordResets.FirstOrDefaultAsync(c => c.UserId == user.LoginId);

            if (reset == null)
            {
                result.Message = "Virheellinen pyyntö";
                result.Success = false;
                return result; // Koodia/PasswordResets objektia ei löydy
            }

            if (reset.ExpiresAt < DateTime.UtcNow)
            {
                result.Message = "Virheellinen pyyntö";
                result.Success = false;
                db.PasswordResets.Remove(reset);
                db.SaveChanges();
                return result;//Koodi on vanhentunut
            }


            if (reset.FailedAttempts >= 5)
            {
                result.Message = "Virheellinen pyyntö";
                result.Success = false;
                db.PasswordResets.Remove(reset);
                db.SaveChanges();
                return result;//Liian monta yritystä
            }

            var incomingHash = (request.ResetCode);


            var valid =  _userservice.VerifyPassword(reset.CodeHash, incomingHash);


            if (!valid)
            {
                reset.FailedAttempts++;
                db.PasswordResets.Update(reset);
                result.Message = "Virheellinen pyyntö";
                result.Success = false;
                db.SaveChanges();
                return result;//Väärä Koodi
            }

            
            //validi koodi ja käyttäjä
            user.Pword = _userservice.HashPassword(request.NewPassword);
            db.Update(user);
            db.PasswordResets.Remove(reset);
            
            await db.SaveChangesAsync();
            await _emailservice.SendEmailAsync(user.Email, "Salasanasi on vaihdettu", "Salasanasi vaihdettiin, jos tämä et ollut sinä ota yhteyttä tukipalveluumme");
            result.Message = "reset Ok";
            result.Success = true;
            return result;
        }

    }
}
