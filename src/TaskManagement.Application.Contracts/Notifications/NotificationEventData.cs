using System;

namespace TaskManagement.Notifications
{
    public class NotificationEventData
    {
        public Guid ReceiverId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }
}