using BLL.Helpers;
using DAL.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SignalR_Training.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly AppSettings _appSettings;
        public AccountController(UserManager<User> userManager, AppSettings appSettings)
        {
            _userManager = userManager;
            _appSettings = appSettings;
        }

        public class LoginData
        {
            public string UserName { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginData loginData)
        {
            string username = loginData.UserName, password = loginData.Password;
            User user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return NotFound();
            }            
            if (await _userManager.CheckPasswordAsync(
                user, password))
            {
                var response = new
                {
                    access_token = CreateJWT(GetIdentity(new List<Claim>() {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, user.Id)})),
                    username = user.UserName
                };
                return Ok(response);                
            }
            else
            {                
                return NotFound();
            }
        }

        private string CreateJWT(ClaimsIdentity claimsIdentity)
        {
            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                issuer: _appSettings.Issuer,
                audience: _appSettings.Audience,
                notBefore: now,
                claims: claimsIdentity.Claims,
                expires: now.Add(TimeSpan.FromMinutes(_appSettings.Lifetime)),
                signingCredentials: new SigningCredentials(new
                SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(_appSettings.Secret)),
                SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        private ClaimsIdentity GetIdentity(List<Claim> claims, string authenticationType = "Token")
        {
            ClaimsIdentity claimsIdentity =
            new ClaimsIdentity(claims, authenticationType, ClaimsIdentity.DefaultNameClaimType,
                ClaimsIdentity.DefaultRoleClaimType);
            return claimsIdentity;
        }
    }
}
