using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpiskelijanKuvapankki2_0.Models;
using OpiskelijanKuvapankki2_0.Services.Interfaces;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using OpiskelijanKuvapankki2_0.Services;


namespace OpiskelijanKuvaPankki.Services

{
    public class AuthenticateService : IAuthenticateService
    {

        private readonly OpiskelijanKuvapankki2_0Context db;

        private readonly AppSettings _appSettings;

        private readonly UserService _userservice;
        public AuthenticateService(IOptions<AppSettings> appSettings, OpiskelijanKuvapankki2_0Context okc, UserService userservice)
        {
            _appSettings = appSettings.Value;
            db = okc;
            _userservice = userservice;
        }


        //
        public AuthResponse Authenticate(string email, string pword)
        {

            var foundUser = db.Logins.SingleOrDefault(x => x.Email == email);

            if (foundUser == null)
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

    }
}
