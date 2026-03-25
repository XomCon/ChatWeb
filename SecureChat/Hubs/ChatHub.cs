using Microsoft.AspNetCore.SignalR;

namespace SecureChat.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinRoom(string roomName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        public async Task SendMessageToRoom(string roomName, string sender, string message)
        {
            await Clients.Group(roomName).SendAsync("ReceiveMessage", sender, message);
        }
    }
}