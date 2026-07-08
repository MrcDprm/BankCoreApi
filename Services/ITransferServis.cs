using System;
using System.Threading.Tasks;

namespace BankCoreApi.Services;

public interface ITransferServis
{
    Task<bool> TransferYapAsync(Guid gonderenHesapId, Guid aliciHesapId, decimal Amount, string aciklama);
}
