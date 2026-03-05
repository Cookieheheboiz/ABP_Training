using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Localization;
using TaskManagement.Notifications;
using TaskManagement.Projects;
using TaskManagement.Tasks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace TaskManagement.BackgroundWorkers
{
    public class TaskDeadlineWorker : AsyncPeriodicBackgroundWorkerBase
    {
        private readonly IStringLocalizer<TaskManagementResource> L;
        public TaskDeadlineWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory,
        IStringLocalizer<TaskManagementResource> localizer)
        : base(timer, serviceScopeFactory)
        {
            L = localizer;
            Timer.Period = 10000;
        }

        protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
        {
            // Lấy công cụ quản lý Unit of Work từ hệ thống
            var uowManager = workerContext.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();

            // Bọc toàn bộ logic lấy và lưu dữ liệu vào bên trong khối using
            using (var uow = uowManager.Begin())
            {
                using (CultureHelper.Use("vi"))
                {
                    var taskRepository = workerContext.ServiceProvider.GetRequiredService<ITaskItemRepository>();
                    var projectRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<Project, Guid>>();
                    var notificationRepository = workerContext.ServiceProvider.GetRequiredService<IRepository<AppNotification, Guid>>();
                    var userManager = workerContext.ServiceProvider.GetRequiredService<IdentityUserManager>();
                    var localEventBus = workerContext.ServiceProvider.GetRequiredService<ILocalEventBus>();

                    var nowUtc = DateTime.UtcNow;
                    var next24hUtc = nowUtc.AddHours(24);

                    var query = await taskRepository.GetQueryableAsync();
                    var pendingTasks = await query.Include(x => x.Assignees)
                        .Where(x => x.Status != TaskManagement.Tasks.TaskStatus.Completed && x.DueDate.HasValue)
                        .ToListAsync();

                    var admins = await userManager.GetUsersInRoleAsync("admin");
                    var adminIds = admins.Select(a => a.Id).ToList();

                    foreach (var task in pendingTasks)
                    {
                        var isOverdue = task.DueDate.Value <= nowUtc;
                        var isNearDeadline = !isOverdue && task.DueDate.Value <= next24hUtc;

                        if (!isOverdue && !isNearDeadline) continue;

                        string title = isOverdue
                            ? L["TaskOverdueTitle"]
                            : L["TaskNearDeadlineTitle"];

                        string timeStr = task.DueDate.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

                        string message = isOverdue
                            ? L["TaskOverdueMessage", task.Title, timeStr]
                            : L["TaskNearDeadlineMessage", task.Title, timeStr];

                        var targetUrl = $"/projects/{task.ProjectId}";

                        var alreadySent = await notificationRepository.AnyAsync(n =>
                            n.TargetUrl == targetUrl &&
                            n.Message == message);

                        if (alreadySent) continue;

                        var project = await projectRepository.GetAsync(task.ProjectId);
                        var receivers = new HashSet<Guid>();

                        foreach (var assignee in task.Assignees) receivers.Add(assignee.UserId);
                        receivers.Add(project.ManagerId);
                        foreach (var adminId in adminIds) receivers.Add(adminId);

                        var notifications = receivers.Select(userId =>
                            new AppNotification(Guid.NewGuid(), userId, title, message, targetUrl, "Task")
                        ).ToList();

                        if (notifications.Any())
                        {
                            await notificationRepository.InsertManyAsync(notifications, autoSave: true);

                            foreach (var userId in receivers)
                            {
                                await localEventBus.PublishAsync(new NotificationEventData
                                {
                                    ReceiverId = userId,
                                    Title = title,
                                    Message = message
                                });
                            }
                        }
                    }
                }

                await uow.CompleteAsync();
            }
        }
    }
}