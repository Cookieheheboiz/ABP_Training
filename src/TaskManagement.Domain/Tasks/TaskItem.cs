using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Tasks
{
    public class TaskItem : FullAuditedAggregateRoot<Guid>
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskStatus Status { get; set; }
        public Guid? AssignedUserId { get; set; }

        protected TaskItem()
        {
        }

        public TaskItem(Guid id, string title, string description, TaskStatus status, Guid? assignedUserId = null)
            : base(id)
        {
            Title = title;
            Description = description;
            Status = status;
            AssignedUserId = assignedUserId;
        }
    }
}
