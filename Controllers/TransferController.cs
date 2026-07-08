using BankCoreApi.Controllers.Dtos;
using BankCoreApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankCoreApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransferController : ControllerBase
{
    private readonly ITransferServis _transferServis;

    public TransferController(ITransferServis transferServis)
    {
        _transferServis = transferServis;
    }

    [HttpPost]
    public async Task<IActionResult> TransferYap([FromBody] TransferIstek istek)
    {
        if (istek.Amount <= 0)
        {
            return BadRequest("Transfer miktari 0'dan buyuk olmalidir.");
        }

        if (istek.GonderenHesapId == istek.AliciHesapId)
        {
            return BadRequest("Gonderen ve alici hesabi ayni olamaz.");
        }

        try
        {
            await _transferServis.TransferYapAsync(
                istek.GonderenHesapId,
                istek.AliciHesapId,
                istek.Amount,
                istek.Aciklama ?? string.Empty);

            return Ok(new { mesaj = "Transfer basariyla tamamlandi." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mesaj = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { mesaj = "Beklenmeyen bir hata olustu." });
        }
    }
}
