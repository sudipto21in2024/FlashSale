using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BoltTickets.API.Hubs;

public class TicketHub : Hub
{
    private readonly ILogger<TicketHub> _logger;

    public TicketHub(ILogger<TicketHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public async Task JoinTicketGroup(string userId)
    {
        _logger.LogInformation($"Client {Context.ConnectionId} joining group: {userId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
         await Clients.Group(userId).SendAsync("test-group-message", new { 
        Message = "You successfully joined the group!",
        UserId = userId 
    });
    }
}
