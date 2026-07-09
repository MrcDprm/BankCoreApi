using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BankCoreApi.Hubs;

[Authorize]
public class NotificationHub : Hub
{
}
