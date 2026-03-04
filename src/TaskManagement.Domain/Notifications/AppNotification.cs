using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Notifications
{
    public class AppNotification : CreationAuditedAggregateRoot<Guid>
    {
        public Guid ReceiverId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string TargetUrl { get; set; }
        public bool IsRead { get; set; }
        public string NotificationType { get; set; }

        protected AppNotification() { }

        public AppNotification(Guid id, Guid receiverId, string title, string message, string targetUrl, string notificationType)
            : base(id)
        {
            ReceiverId = receiverId;
            Title = title;
            Message = message;
            TargetUrl = targetUrl;
            NotificationType = notificationType;
            IsRead = false;
        }
    }
}