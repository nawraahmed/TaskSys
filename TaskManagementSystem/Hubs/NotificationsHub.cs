using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;


namespace SignalR.Hubs
{
    public class NotificationsHub : Hub
    {
        IHubContext<NotificationsHub> _hubContext;
        public NotificationsHub(IHubContext<NotificationsHub> hubcontext)
        {
            _hubContext = hubcontext;
        }
        public async Task NotificationsHubBroadcast(List<TaskManagementSystem.Models.Notification> notifications)
        {
            await _hubContext.Clients.All.SendAsync("getUpdatedNotifications", notifications);

        }
    }
}
