using BankCoreApi.Controllers.Dtos;
using BankCoreApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankCoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HesapController : ControllerBase
{
    private readonly IHesapServis _hesapServis;

    public HesapController(IHesapServis hesapServis)
    {
        _hesapServis = hesapServis;
    }

    [HttpPost("olustur")]
    public async Task<IActionResult> Olustur([FromBody] HesapOlusturIstek istek)
    {
        if (string.IsNullOrWhiteSpace(istek.HesapSahibiAd))
        {
            return BadRequest("Hesap sahibi adi bos olamaz.");
        }

        var hesap = await _hesapServis.HesapOlusturAsync(istek.HesapSahibiAd);
        return Ok(hesap);
    }

    [HttpGet("{id:guid}/bakiye")]
    public async Task<IActionResult> BakiyeGetir([FromRoute] Guid id)
    {
        var bakiye = await _hesapServis.BakiyeGetirAsync(id);
        return Ok(new { bakiye = bakiye });
    }
}
