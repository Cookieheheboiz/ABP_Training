using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Notifications
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string TargetUrl { get; set; }
        public bool IsRead { get; set; }
        public string NotificationType { get; set; }
        public DateTime CreationTime { get; set; }
    }
}