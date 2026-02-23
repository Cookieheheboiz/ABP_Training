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

        protected TaskAssignee() { }

        public TaskAssignee(Guid taskId, Guid userId)
        {
            Id = Guid.NewGuid();

            TaskId = taskId;
            UserId = userId;
        }

        public override object[] GetKeys()
        {
            return new object[] { Id };
        }
    }
}