using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BankCoreApi.Controllers.Dtos;
using BankCoreApi.Data;
using BankCoreApi.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BankCoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly BankaDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(BankaDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("kayit")]
    public async Task<IActionResult> Kayit([FromBody] KayitIstek istek)
    {
        if (string.IsNullOrWhiteSpace(istek.HesapSahibiAd) ||
            string.IsNullOrWhiteSpace(istek.Email) ||
            string.IsNullOrWhiteSpace(istek.Sifre))
        {
            return BadRequest("Hesap sahibi adi, email ve sifre bos olamaz.");
        }

        var mevcutHesap = await _context.Hesaplar
            .FirstOrDefaultAsync(h => h.Email == istek.Email);

        if (mevcutHesap is not null)
        {
            return BadRequest("Bu email adresi ile kayitli bir hesap zaten var.");
        }

        var randomDigits = string.Join("", Enumerable.Range(0, 18).Select(_ => Random.Shared.Next(0, 10).ToString()));
        var sifreHash = BCrypt.Net.BCrypt.HashPassword(istek.Sifre);

        var hesap = new Hesap
        {
            Id = Guid.NewGuid(),
            HesapNo = "TR" + randomDigits,
            HesapSahibiAd = istek.HesapSahibiAd,
            Email = istek.Email,
            SifreHash = sifreHash,
            CreatedAt = DateTime.UtcNow
        };

        _context.Hesaplar.Add(hesap);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            hesap.Id,
            hesap.HesapNo,
            hesap.HesapSahibiAd,
            hesap.Email,
            hesap.CreatedAt
        });
    }

    [HttpPost("giris")]
    public async Task<IActionResult> Giris([FromBody] GirisIstek istek)
    {
        if (string.IsNullOrWhiteSpace(istek.Email) || string.IsNullOrWhiteSpace(istek.Sifre))
        {
            return BadRequest("Email ve sifre bos olamaz.");
        }

        var hesap = await _context.Hesaplar
            .FirstOrDefaultAsync(h => h.Email == istek.Email);

        if (hesap is null || !BCrypt.Net.BCrypt.Verify(istek.Sifre, hesap.SifreHash))
        {
            return Unauthorized();
        }

        var jwtKey = _configuration["Jwt:Key"];
        var jwtIssuer = _configuration["Jwt:Issuer"];
        var jwtAudience = _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey) ||
            string.IsNullOrWhiteSpace(jwtIssuer) ||
            string.IsNullOrWhiteSpace(jwtAudience))
        {
            return StatusCode(500, new { mesaj = "JWT yapilandirmasi eksik." });
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, hesap.Id.ToString()),
            new Claim(ClaimTypes.Email, hesap.Email)
        };

        if (hesap.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return Ok(new
        {
            token = token,
            hesapId = hesap.Id
        });
    }
}
