using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace TaskManagement.Tasks;
public class EfCoreTaskItemRepository
    : EfCoreRepository<TaskManagementDbContext, TaskItem, Guid>, ITaskItemRepository
{
    public EfCoreTaskItemRepository(IDbContextProvider<TaskManagementDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        var dbSet = await GetDbSetAsync();

        await dbSet
            .Where(t => t.Id == id)
            .ExecuteDeleteAsync();
    }

    public async Task UpdateStatusAsync(Guid id, TaskStatus status)
    {
        var dbSet = await GetDbSetAsync();

        await dbSet
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, status));
    }

    public async Task<List<TaskItem>> GetTasksByAssignedUserAndStatusAsync(
        Guid assignedUserId,
        TaskStatus status)
    {
        var dbSet = await GetDbSetAsync();

        return await dbSet
            .Where(t => t.AssignedUserId == assignedUserId && t.Status == status)
            .OrderByDescending(t => t.CreationTime)
            .ToListAsync();
    }
}
