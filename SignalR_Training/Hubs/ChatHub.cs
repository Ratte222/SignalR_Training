using Microsoft.AspNetCore.SignalR;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SignalR_Training.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", $"{user} ({Context.ConnectionId})", message);
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.Others.SendAsync("Notify", $"{Context.ConnectionId} entered the chat");
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await Clients.All.SendAsync("Notify", $"{Context.ConnectionId} escaped the chat");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
