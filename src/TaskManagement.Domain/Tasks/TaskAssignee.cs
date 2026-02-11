using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Domain.Entities;

namespace TaskManagement.Tasks
{
    public class TaskAssignee : Entity<Guid>
    {
        public Guid TaskId { get; set; }
        public Guid UserId { get; set; }

        // Constructor
        public TaskAssignee(Guid taskId, Guid userId)
        {
            TaskId = taskId;
            UserId = userId;
        }
    }
}