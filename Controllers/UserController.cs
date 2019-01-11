using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using webshop.Models;


namespace webshop.Controllers
{
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly DbConnectionContext _context;
        public UserController(DbConnectionContext context)
        {
            this._context = context;
        }

        private readonly RandomNumberGenerator _rng;

        public virtual string HashPassword(User user, string password)
        {
            return Convert.ToBase64String(HashPasswordV2(password, _rng));
        }
        private static byte[] HashPasswordV2(string password, RandomNumberGenerator rng)
        {
            const KeyDerivationPrf Pbkdf2Prf = KeyDerivationPrf.HMACSHA1; // default for Rfc2898DeriveBytes
            const int Pbkdf2IterCount = 1000; // default for Rfc2898DeriveBytes
            const int Pbkdf2SubkeyLength = 256 / 8; // 256 bits
            const int SaltSize = 128 / 8; // 128 bits

            // Produce a version 2 text hash.
            byte[] salt = new byte[SaltSize];
            rng.GetBytes(salt);
            byte[] subkey = KeyDerivation.Pbkdf2(password, salt, Pbkdf2Prf, Pbkdf2IterCount, Pbkdf2SubkeyLength);

            var outputBytes = new byte[1 + SaltSize + Pbkdf2SubkeyLength];
            outputBytes[0] = 0x00; // format marker
            Buffer.BlockCopy(salt, 0, outputBytes, 1, SaltSize);
            Buffer.BlockCopy(subkey, 0, outputBytes, 1 + SaltSize, Pbkdf2SubkeyLength);
            return outputBytes;
        }

        // GET api/Products
        [HttpGet]
        public IActionResult Get()
        {
            var result = this._context.Users.OrderBy(user => user.Id).Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email
            });

            return new OkObjectResult(result);
        }

        [HttpGet("{id}")]
        public IQueryable Get(int id)
        {
            var result = this._context.Users.Select(user => new
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                BirthDate = user.BirthDate,
                Email = user.Email
            }).Where(user => user.Id == id);

            return result;
        }

        [HttpGet("getdata/{id}")]
        public IActionResult GetData(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }
            return new ObjectResult(user);
        }



        [HttpGet("confirmation/{token}")]
        public IActionResult CheckRegisterConfirmationToken(string token)
        {
            //Check if confirmation token exists && that token is not turned in yet
            ConfirmationMail result = _context.ConfirmationMails.FirstOrDefault(cMail => cMail.ConfirmationToken == token && cMail.AccountStatus == 0);
            if (result != null)
            {
                result.AccountStatus = 1;
                _context.SaveChanges();

                return new OkObjectResult(true);
            }

            return new OkObjectResult(false);

        }

        [HttpPut("ChangePassword")]
        public IActionResult ChangePassword([FromBody] User user)
        {
            User userToUpdate = _context.Users.FirstOrDefault(u => u.Id == user.Id);
            if (userToUpdate != null)
            {
                userToUpdate.Password = user.Password;

                _context.SaveChanges();

                return new OkObjectResult(new { isError = false, userUpdated = true, response = "Gebruiker is aangepast." });
            }

            return new OkObjectResult(new { isError = true, userUpdated = false, response = "Gebruiker bestaat niet." });
        }

        [HttpPut("UpdateUser")]
        public IActionResult UpdateUser([FromBody] User user)
        {
            User userToUpdate = _context.Users.FirstOrDefault(u => u.Id == user.Id);
            if (userToUpdate != null)
            {
                userToUpdate.FirstName = user.FirstName;
                userToUpdate.LastName = user.LastName;

                userToUpdate.BirthDate = user.BirthDate;
                userToUpdate.Email = user.Email;

                _context.SaveChanges();

                return new OkObjectResult(new { isError = false, userUpdated = true, response = "Gebruiker is aangepast." });
            }

            return new OkObjectResult(new { isError = true, userUpdated = false, response = "Gebruiker bestaat niet." });
        }

        [HttpPost("AddUserAddress")]
        public IActionResult AddUserAddress([FromBody] UserAddress userAddress)
        {
            if (userAddress == null)
            {
                return new OkObjectResult(new { isError = true, addresAdded = false, response = "Adres is niet goed ingevuld." });
            }

            User user = _context.Users.FirstOrDefault(u => u.Id == userAddress.UserId);
            if (user != null)
            {
                List<UserAddress> userAddresses = _context.UserAddresses.Where(uAddress => uAddress.UserId == user.Id && uAddress.Current == 1).Include(uAddress => uAddress.Address).ToList();
                foreach (var uAddress in userAddresses)
                {
                    uAddress.Current = 0;
                    uAddress.Address.DateTo = userAddress.Address.DateFrom;
                }

                userAddress.User = user;
                _context.UserAddresses.Add(userAddress);
                _context.SaveChanges();

                return new OkObjectResult(new { isError = false, addresAdded = true, response = "Adres is succesvol toegevoegd." });
            }

            return new OkObjectResult(new { isError = true, addresAdded = false, response = "Gebruiker bestaat niet." });

        }

        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromBody] User u)
        {

            var loginToken = Guid.NewGuid().ToString();

            var result = this._context.Users
            .Where(us => us.Email == u.Email && us.Password == u.Password)
            .Select(us => new { us.Id, us.Email, us.FirstName, token = loginToken }).FirstOrDefault();

            if (result != null)
                return new OkObjectResult(result);

            return new ConflictObjectResult(new { msg = "Inloggegevens zijn incorrect" });

        }
        [HttpGet("GetUserAdress/{id}")]
        public IActionResult GetUserAdress(int id)
        {
            var result = this._context.Users
                        .Select(u => new
                        {
                            u.Id,
                            u.FirstName,
                            u.LastName,
                            u.BirthDate,
                            u.Email,
                            Addresses = u.Addresses.Select(a => a).Where(a => a.Current == 1).Select(ad => ad.Address).Single()
                        }).Where(u => u.Id == id);

            return new OkObjectResult(result);


        }

        [HttpGet("GetUserByIdForUserProfile/{id}")]
        public IQueryable GetUserByIdForUserProfile(int id)
        {
            var result = this._context.Users
                        .Select(u => new
                        {
                            u.Id,
                            u.FirstName,
                            u.LastName,
                            u.BirthDate,
                            u.Email,
                            u.Password,
                            CurrentAddress = u.Addresses.Where(a => a.Current == 1).Select(a => a.Address).Single()
                        }).Where(u => u.Id == id);

            return result;
        }

        [HttpGet("GetUserById/{id}")]
        public IQueryable GetUserById(int id)
        {
            var result = this._context.Users
                        .Select(u => new
                        {
                            u.Id,
                            u.FirstName,
                            u.LastName,
                            u.BirthDate,
                            u.Email,
                            u.Password,
                            Addresses = u.Addresses.Select(a => a).Select(ad => ad.Address),
                        }).Where(u => u.Id == id);

            return result;
        }

        [HttpPost]
        public IActionResult Post([FromBody]UserAddress u)
        {
            if (u != null && u.User != null)
            {
                string confirmationTokenGuid = Guid.NewGuid().ToString();

                var emailAlreadyExists = _context.Users.Any(user => user.Email.ToLower() == u.User.Email.ToLower());
                if (!emailAlreadyExists)
                {
                    bool emailHasBeenSend = this.SendEmail(u.User, confirmationTokenGuid);
                    if (emailHasBeenSend)
                    {
                        ConfirmationMail confirmationMail = new ConfirmationMail();
                        confirmationMail.User = u.User;
                        confirmationMail.AccountStatus = 0;// eerst 0, als hij in de mail link klikt dan 1
                        confirmationMail.ConfirmationToken = confirmationTokenGuid;

                        u.User.ConfirmationMail = confirmationMail;

                        u.Current = 1;
                        _context.UserAddresses.Add(u);

                        _context.SaveChanges();
                        return new OkObjectResult(new { emailSend = true, isError = false, response = "Wij hebben een validatie mail gestuurd naar: " + u.User.Email + ". Klik op de link in deze mail om uw account aan te maken" });
                    }

                    return new OkObjectResult(new { emailSend = false, isError = true, response = "De email kon niet verzonden worden, de email bestaat niet" });

                }

                //User already exists
                return new ConflictObjectResult(new { emailSend = false, isError = true, response = "Email bestaat al" });
            }

            //Information was incorrect
            return new ConflictObjectResult(new { emailSend = false, isError = true, response = "De gegeven informatie is niet correct" });
        }

        private bool SendEmail(User user, string confirmationToken)
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("<html><head><title>Confirmation mail:</title></head><body>");
                sb.Append("<p>TO: " + user.FirstName + " " + user.LastName + "/" + "</p><br/>");

                sb.Append("<p>Here is the link:</p><br/>");
                sb.Append("<p>" + "<a href=" + "https://localhost:5001/confirmation/" + confirmationToken + "" + ">" + "Confirmation link" + "</a></p><br/>");
                sb.Append("PLEASE DO NOT REPLY TO THIS MESSAGE AS IT IS FROM AN UNATTENDED MAILBOX. ANY REPLIES TO THIS EMAIL WILL NOT BE RESPONDED TO OR FORWARDED. THIS SERVICE IS USED FOR OUTGOING EMAILS ONLY AND CANNOT RESPOND TO INQUIRIES.");

                SmtpClient SmtpServer = new SmtpClient("smtp.live.com");

                var mail = new MailMessage();
                mail.From = new MailAddress("mediamaniawebshop@hotmail.com");
                mail.To.Add(user.Email);//0946586@hr.nl
                mail.Subject = "confirmation mail";
                mail.IsBodyHtml = true;

                string htmlBody;

                htmlBody = "@ Hello! " + user.FirstName + "\n your MediaMania account is about to be created, please click the following activation link: \n " + "<a href=\"www.google.com\">login</a>";
                mail.Body = sb.ToString();

                SmtpServer.Port = 587;
                SmtpServer.UseDefaultCredentials = false;
                SmtpServer.Credentials = new System.Net.NetworkCredential("mediamaniawebshop@hotmail.com", "mediamania1!");
                SmtpServer.EnableSsl = true;
                SmtpServer.Send(mail);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsEmailValid(string emailAddress)
        {
            try
            {
                MailAddress eMailAddress = new MailAddress(emailAddress);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}