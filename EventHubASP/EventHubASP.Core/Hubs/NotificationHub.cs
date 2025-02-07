using EventHubASP.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubASP.Core.Hubs
{
    public class NotificationHub: Hub
    {
        public async Task SendNotification(string userId, Notification notification)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", notification);
        }
    }
}
