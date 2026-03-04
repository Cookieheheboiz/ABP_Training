using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Notifications
{
    [Authorize]
    public class NotificationAppService : ApplicationService, INotificationAppService
    {
        private readonly IRepository<AppNotification, Guid> _notificationRepository;

        public NotificationAppService(IRepository<AppNotification, Guid> notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<List<NotificationDto>> GetMyNotificationsAsync()
        {
            var userId = CurrentUser.Id.Value;
            var query = await _notificationRepository.GetQueryableAsync();

            var notifications = await AsyncExecuter.ToListAsync(
                query.Where(n => n.ReceiverId == userId)
                     .OrderByDescending(n => n.CreationTime)
                     .Take(20)
            );

            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                TargetUrl = n.TargetUrl,
                IsRead = n.IsRead,
                NotificationType = n.NotificationType,
                CreationTime = n.CreationTime
            }).ToList();
        }

        public async Task MarkAsReadAsync(Guid id)
        {
            var notification = await _notificationRepository.GetAsync(id);
            if (notification.ReceiverId == CurrentUser.Id)
            {
                notification.IsRead = true;
                await _notificationRepository.UpdateAsync(notification);
            }
        }

        public async Task MarkAllAsReadAsync()
        {
            var userId = CurrentUser.Id.Value;
            var query = await _notificationRepository.GetQueryableAsync();

            var unreadNotifications = await AsyncExecuter.ToListAsync(
                query.Where(n => n.ReceiverId == userId && !n.IsRead)
            );

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _notificationRepository.UpdateManyAsync(unreadNotifications);
        }
    }
}