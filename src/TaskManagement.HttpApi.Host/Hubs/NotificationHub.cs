using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Users; 

namespace TaskManagement.Hubs
{
    [Authorize]
    public class NotificationHub : AbpHub
    {
        private readonly ICurrentUser _currentUser;

        public NotificationHub(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            if (_currentUser.Id.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, _currentUser.Id.Value.ToString().ToLowerInvariant());
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (_currentUser.Id.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, _currentUser.Id.Value.ToString().ToLowerInvariant());
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}