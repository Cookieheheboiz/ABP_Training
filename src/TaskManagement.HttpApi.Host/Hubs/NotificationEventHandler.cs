using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging; // 👈 Import thêm Logger
using System.Threading.Tasks;
using TaskManagement.Notifications;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace TaskManagement.Hubs
{
    public class NotificationEventHandler : ILocalEventHandler<NotificationEventData>, ITransientDependency
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationEventHandler> _logger; // 👈 Khai báo biến ghi log

        public NotificationEventHandler(
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationEventHandler> logger) // 👈 Inject Logger vào
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task HandleEventAsync(NotificationEventData eventData)
        {
            // 1. Máy định vị: In ra màn hình console đen của Backend để xem EventBus có chạy không
            _logger.LogInformation($"[SIGNALR DEBUG] Đang gửi thông báo '{eventData.Title}' tới User ID: {eventData.ReceiverId}");

            await _hubContext.Clients
                .Group(eventData.ReceiverId.ToString())
                .SendAsync("ReceiveNotification", eventData.Title, eventData.Message);
        }
    }
}