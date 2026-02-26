using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace TaskManagement.Calendars
{
    public class CalendarAppService : ApplicationService, ICalendarAppService
    {
        private readonly IRepository<TaskItem, Guid> _taskRepository;
        private readonly IRepository<IdentityUser, Guid> _userRepository;

        public CalendarAppService(
            IRepository<TaskItem, Guid> taskRepository,
            IRepository<IdentityUser, Guid> userRepository)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
        }

        public async Task<List<CalendarTaskDto>> GetCalendarTasksAsync(GetCalendarTasksInput input)
        {
            var queryable = await _taskRepository.GetQueryableAsync();

            queryable = queryable.Include(x => x.Project)
                .Include(x => x.Assignees);

            if (input.ProjectId.HasValue)
            {
                queryable = queryable.Where(x => x.ProjectId == input.ProjectId.Value);
            }

            if (!CurrentUser.IsInRole("admin"))
            {
                var currentUserId = CurrentUser.Id;
                queryable = queryable.Where(x =>
                    x.Project.CreatorId == currentUserId ||
                    x.Project.ManagerId == currentUserId ||
                    x.Project.Members.Any(m => m.UserId == currentUserId) ||
                    x.Assignees.Any(a => a.UserId == currentUserId)
                );
            }

            queryable = queryable.Where(x =>
                x.DueDate.HasValue &&
                x.DueDate.Value >= input.StartDate &&
                x.DueDate.Value <= input.EndDate
            );

            var tasks = await AsyncExecuter.ToListAsync(queryable);

            var allUserIds = tasks.SelectMany(t => t.Assignees.Select(a => a.UserId)).Distinct().ToList();
            var userDictionary = new Dictionary<Guid, string>();

            if (allUserIds.Any())
            {
                var userQueryable = await _userRepository.GetQueryableAsync();

                var userList = await AsyncExecuter.ToListAsync(
                    userQueryable.Where(u => allUserIds.Contains(u.Id))
                );

                userDictionary = userList.ToDictionary(u => u.Id, u => u.UserName);
            }
            return tasks.Select(t => new CalendarTaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                DueDate = t.DueDate,
                Status = t.Status,
                IsApproved = t.IsApproved,
                ProjectId = t.ProjectId,
                ProjectName = t.Project?.Name,
                AssignedUserNames = t.Assignees
                    .Where(a => userDictionary.ContainsKey(a.UserId))
                    .Select(a => userDictionary[a.UserId])
                    .ToList()
            }).ToList();
        }
    }
}