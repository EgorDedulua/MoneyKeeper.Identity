using Microsoft.Extensions.Options;
using MoneyKeeper.Identity.Application.Common.Interfaces;
using MoneyKeeper.Identity.Core.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;

namespace MoneyKeeper.Identity.Infrastructure.Auth
{
    public class JwtService : IJwtService
    {
        private readonly IOptions<JwtOptions> _options;

        public JwtService(IOptions<JwtOptions> options)
        {
            _options = options;
        }

        public string GenerateAccessToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            JwtSecurityToken token = new JwtSecurityToken(
                expires: DateTime.UtcNow.Add(_options.Value.Expires),
                claims: claims,
                issuer: _options.Value.Issuer,
                audience: _options.Value.Audience,
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_options.Value.SecretKey)),
                    SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        public string ComputeHash(string rawData)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToBase64String(bytes);
        }
    }
}
