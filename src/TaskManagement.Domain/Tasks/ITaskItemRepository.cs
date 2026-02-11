using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace TaskManagement.Tasks
{
    public interface ITaskItemRepository : IRepository<TaskItem, Guid>
    {
        Task<List<TaskItem>> GetTasksByAssignedUserAndStatusAsync(
            Guid assignedUserId,
            TaskStatus status
        );
        Task UpdateStatusAsync(Guid id, TaskStatus status);
        Task DeleteTaskAsync(Guid id);
    }
}
