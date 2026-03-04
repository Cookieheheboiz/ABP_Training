using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Notifications;
using TaskManagement.Permissions;
using TaskManagement.Projects;
using TaskManagement.Services.Base;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using static TaskManagement.Permissions.TaskManagementPermissions;


namespace TaskManagement.Tasks
{
    [RemoteService(IsEnabled = false)]
    public class TaskAppService : MyCrudAppService<
            TaskItem,
            TaskDto,
            Guid,
            GetTasksInput,
            CreateUpdateTaskDto>,
            ITaskAppService
    {
        private readonly IIdentityUserRepository _userRepository;
        private readonly ITaskItemRepository _taskItemRepository;
        private readonly IRepository<Project, Guid> _projectRepository;
        private readonly IRepository<AppNotification, Guid> _notificationRepository;
        private readonly IdentityUserManager _userManager;
        private readonly ILocalEventBus _localEventBus; 
        public TaskAppService(ITaskItemRepository repository, IIdentityUserRepository userRepository, IRepository<Project, Guid> projectRepository, IRepository<AppNotification, Guid> notificationRepository, IdentityUserManager userManager, ILocalEventBus localEventBus
            )
             : base(repository)
        {
            _userRepository = userRepository;
            _taskItemRepository = repository;
            _projectRepository = projectRepository;
            _notificationRepository = notificationRepository;
            _userManager = userManager;
            _localEventBus = localEventBus;

            GetPolicyName = "TaskManagement.Tasks";
            GetListPolicyName = "TaskManagement.Tasks";
            CreatePolicyName = "TaskManagement.Tasks.Create";
            UpdatePolicyName = "TaskManagement.Tasks.Update";
            DeletePolicyName = "TaskManagement.Tasks.Delete";
        }

        public override async Task<TaskDto> CreateAsync(CreateUpdateTaskDto input)
        {
            await CheckPolicyAsync(CreatePolicyName);
            var projectQuery = await _projectRepository.GetQueryableAsync();
            var project = await projectQuery.Include(x => x.Members)
                                            .FirstOrDefaultAsync(x => x.Id == input.ProjectId);

            if (project == null) throw new UserFriendlyException(L["Not Found Project"]);

            var isDuplicate = await Repository.AnyAsync(x =>
                x.ProjectId == input.ProjectId &&
                x.Title == input.Title &&
                x.Description == input.Description &&
                x.DueDate.HasValue == input.DueDate.HasValue &&
                (!x.DueDate.HasValue || (
                    x.DueDate.Value.Year == input.DueDate.Value.Year &&
                    x.DueDate.Value.Month == input.DueDate.Value.Month &&
                    x.DueDate.Value.Day == input.DueDate.Value.Day &&
                    x.DueDate.Value.Hour == input.DueDate.Value.Hour &&
                    x.DueDate.Value.Minute == input.DueDate.Value.Minute
                ))
            );

            if (isDuplicate)
            {
                throw new UserFriendlyException(L["There already exists a job with identical content, description, and deadline in this project!"]);
            }

            var validUserIds = project.Members.Select(m => m.UserId).ToList();
            validUserIds.Add(project.ManagerId);

            var currentUserId = CurrentUser.Id;
            bool isApproved = false;

            var task = new TaskItem(
                GuidGenerator.Create(),
                input.Title,
                input.Description,
                input.Status,
                input.ProjectId,
                isApproved,
                input.DueDate
            );

            if (input.AssignedUserIds != null && input.AssignedUserIds.Any())
            {
                foreach (var userId in input.AssignedUserIds)
                {
                    if (!validUserIds.Contains(userId))
                    {
                        throw new UserFriendlyException(L["Tasks can only be assigned to people on this project"]);
                    }
                    task.AddAssignee(userId);
                }
            }

            await Repository.InsertAsync(task, autoSave: true);
            var timeStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            var msg = $"Công việc '{task.Title}' đã được tạo mới vào lúc {timeStr}.";
            await SendTaskNotificationAsync(input.AssignedUserIds?.ToList(), "Task mới", msg, task.ProjectId);

            return ObjectMapper.Map<TaskItem, TaskDto>(task);
        }

        public override async Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input)
        {
            // Trước khi thực hiện nội dung hàm bên dưới thì kiểm tra quyền hiện tại có hợp lệ không
            await CheckPolicyAsync(UpdatePolicyName);

            // Lấy Task từ Database lên
            var query = await Repository.GetQueryableAsync();
            var task = await query.Include(x => x.Assignees)
                                  .FirstOrDefaultAsync(x => x.Id == id);

            if (task == null) throw new EntityNotFoundException(typeof(TaskItem), id);

            // Kiểm tra: Nếu không phải Admin VÀ không phải người tạo -> Báo lỗi
            var project = await _projectRepository.GetAsync(task.ProjectId);
            var isCreator = task.CreatorId == CurrentUser.Id;
            var isAssignee = task.Assignees.Any(x => x.UserId == CurrentUser.Id); // Kiểm tra là những người được giao việc
            var isManager = CurrentUser.IsInRole("admin") || project.ManagerId == CurrentUser.Id;

            if (!isCreator && !isManager && !isAssignee)
            {
                throw new UserFriendlyException(L["TaskUpdateForbidden"]);
            }

            if (isAssignee && !isCreator && !isManager)
            {
                await _taskItemRepository.UpdateStatusAsync(id, input.Status);
                task.Status = input.Status;
            }
            else
            {
                var targetProjectId = task.IsApproved ? task.ProjectId : input.ProjectId;
                var projectQuery = await _projectRepository.GetQueryableAsync();
                var targetProject = await projectQuery.Include(x => x.Members)
                                                      .FirstOrDefaultAsync(x => x.Id == targetProjectId);

                if (targetProject == null) throw new UserFriendlyException(L["The project doesn't exist!"]);

                var validUserIds = targetProject.Members.Select(m => m.UserId).ToList();
                validUserIds.Add(targetProject.ManagerId);

                if (task.IsApproved)
                {
                    task.Status = input.Status;
                    task.Assignees.Clear();
                    if (input.AssignedUserIds != null)
                    {
                        foreach (var userId in input.AssignedUserIds)
                        {
                            if (!validUserIds.Contains(userId)) throw new UserFriendlyException(L["Error: The assigned person is not part of the project!"]);
                            task.AddAssignee(userId);
                        }
                    }
                }
                else
                {
                    var isDuplicate = await Repository.AnyAsync(x => 
                        x.ProjectId == input.ProjectId &&
                        x.Title == input.Title &&
                        x.Id != id &&
                        x.Description == input.Description &&
    
                        x.DueDate.HasValue == input.DueDate.HasValue &&
                        (!x.DueDate.HasValue || (
                            x.DueDate.Value.Year == input.DueDate.Value.Year &&
                            x.DueDate.Value.Month == input.DueDate.Value.Month &&
                            x.DueDate.Value.Day == input.DueDate.Value.Day &&
                            x.DueDate.Value.Hour == input.DueDate.Value.Hour &&
                            x.DueDate.Value.Minute == input.DueDate.Value.Minute
                        ))
                    );

                    if (isDuplicate)
                    {
                        throw new UserFriendlyException(L["DuplicateTaskError"]);
                    }

                    var changes = new List<string>();

                    if (task.Title != input.Title) changes.Add($"Tên công việc thành '{input.Title}'");
                    if (task.Status != input.Status) changes.Add("Trạng thái");
                    if (task.DueDate != input.DueDate) changes.Add("Hạn chót");
                    if (task.Description != input.Description) changes.Add("Mô tả");

                    // Kiểm tra xem danh sách Assignee có bị thay đổi không
                    var oldAssignees = task.Assignees.Select(a => a.UserId).OrderBy(id => id).ToList();
                    var newAssignees = input.AssignedUserIds?.OrderBy(id => id).ToList() ?? new List<Guid>();
                    if (!oldAssignees.SequenceEqual(newAssignees))
                    {
                        changes.Add("Người thực hiện");
                    }

                    // Nếu có bất kỳ sự thay đổi nào thì rải thông báo
                    if (changes.Any())
                    {
                        var timeStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                        var msg = $"Công việc '{task.Title}' đã chỉnh sửa: {string.Join(", ", changes)} vào lúc {timeStr}.";

                        // Gộp cả người cũ và người mới để ai cũng biết task bị đổi
                        var allInvolvedUsers = oldAssignees.Union(newAssignees).ToList();
                        await SendTaskNotificationAsync(allInvolvedUsers, "Cập nhật Task", msg, task.ProjectId);
                    }

                    task.Title = input.Title;
                    task.Description = input.Description;
                    task.Status = input.Status;
                    task.DueDate = input.DueDate;
                    task.ProjectId = input.ProjectId;

                    task.Assignees.Clear();
                    if (input.AssignedUserIds != null)
                    {
                        foreach (var userId in input.AssignedUserIds)
                        {
                            if (!validUserIds.Contains(userId)) throw new UserFriendlyException("Lỗi: Người được giao không thuộc dự án!");
                            task.AddAssignee(userId);
                        }
                    }
                }
            }

            await Repository.UpdateAsync(task, autoSave: true);
            return ObjectMapper.Map<TaskItem, TaskDto>(task);
        }

        public override async Task DeleteAsync(Guid id)
        {
            // Trước khi thực hiện nội dung hàm bên dưới thì kiểm tra quyền hiện tại có hợp lệ không
            await CheckPolicyAsync(DeletePolicyName);

            var task = await Repository.GetAsync(id);
            var oldAssignees = task.Assignees.Select(a => a.UserId).ToList();
            var project = await _projectRepository.GetAsync(task.ProjectId);

            var currentUserId = CurrentUser.Id;
            var isCreator = task.CreatorId == currentUserId;
            var isManager = CurrentUser.IsInRole("admin") || project.ManagerId == currentUserId;

            // Assignee KHÔNG ĐƯỢC PHÉP XÓA (trừ khi đó cũng là Creator)
            if (!isCreator && !isManager)
            {
                throw new UserFriendlyException(L["TaskDeletionForbidden"]);
            }

            await _taskItemRepository.DeleteTaskAsync(id);
            var timeStr = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            var msg = $"Công việc '{task.Title}' đã bị xóa vào lúc {timeStr}.";
            await SendTaskNotificationAsync(oldAssignees, "Xóa Task", msg, task.ProjectId);
        }
        public override async Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input)
        {
            await CheckPolicyAsync(GetListPolicyName);

            var queryable = await Repository.GetQueryableAsync();
            queryable = queryable.Include(x => x.Project)
                         .Include(x => x.Assignees);

            if (input.ProjectId.HasValue)
            {
                queryable = queryable.Where(x => x.ProjectId == input.ProjectId.Value);
            }

            // Logic: Nếu không phải Manager thì chỉ xem task liên quan đến mình
            if (!CurrentUser.IsInRole("admin"))
            {
                var currentUserId = CurrentUser.Id;
                queryable = queryable.Where(x =>
                    x.Project.CreatorId == currentUserId ||
                    x.Project.ManagerId == currentUserId ||                       // Là PM của dự án
                    x.Project.Members.Any(m => m.UserId == currentUserId) ||      // Là thành viên dự án
                    x.Assignees.Any(a => a.UserId == currentUserId) ||            // Được giao Task này
                    x.CreatorId == currentUserId
                );
            }

            // Filter
            var filter = input.FilterText?.ToLower().Trim();
            queryable = queryable
                .WhereIf(!string.IsNullOrWhiteSpace(filter), x =>
                    x.Title.ToLower().Contains(filter) ||
                    (x.Description != null && x.Description.ToLower().Contains(filter))
                )
                .WhereIf(input.Status.HasValue, x => x.Status == input.Status)
                .WhereIf(input.AssignedUserId.HasValue, x => x.Assignees.Any(a => a.UserId == input.AssignedUserId.Value));

            // Count
            var totalCount = await queryable.LongCountAsync();

            // Sort
            if (string.IsNullOrWhiteSpace(input.Sorting))
            {
                input.Sorting = nameof(TaskItem.CreationTime) + " DESC";
            }
            queryable = ApplySorting(queryable, input);
            queryable = ApplyPaging(queryable, input);

            // Execute
            var tasks = await queryable.ToListAsync();
            var taskDtos = ObjectMapper.Map<List<TaskItem>, List<TaskDto>>(tasks);

            // Map tên Users
            var allUserIds = tasks.SelectMany(t => t.Assignees.Select(a => a.UserId)).Distinct().ToList();
            var userDictionary = new Dictionary<Guid, string>();
            if (allUserIds.Any())
            {
                var userList = await _userRepository.GetListByIdsAsync(allUserIds);
                userDictionary = userList.ToDictionary(u => u.Id, u => u.UserName);
            }

            // Tiến hành gán dữ liệu bổ sung vào DTO
            foreach (var dto in taskDtos)
            {
                var originalTask = tasks.First(t => t.Id == dto.Id);

                //1. Phải gán ID dự án
                dto.ProjectId = originalTask.ProjectId;
                dto.ProjectName = originalTask.Project?.Name;

                dto.ProjectManagerId = originalTask.Project?.ManagerId;

                //2. Phải gán danh sách ID người dùng (Để Select đa chọn nhận diện được)
                dto.AssignedUserIds = originalTask.Assignees.Select(a => a.UserId).ToList();

                // Phần lấy tên để hiển thị ở bảng (List) giữ nguyên
                dto.AssignedUserNames = originalTask.Assignees
                    .Where(a => userDictionary.ContainsKey(a.UserId))
                    .Select(a => userDictionary[a.UserId])
                    .ToList();
            }

            return new PagedResultDto<TaskDto>(totalCount, taskDtos);
        }

        public async Task<ProjectTaskStatsDto> GetProjectStatsAsync(Guid projectId)
        {
            var query = await Repository.GetQueryableAsync();

            // Lọc lấy các task thuộc dự án này
            query = query.Where(t => t.ProjectId == projectId);

            // Kéo 3 cột cần thiết về RAM để đếm nhanh (1 lần query duy nhất)
            var tasksData = await AsyncExecuter.ToListAsync(
                query.Select(t => new { t.Status, t.IsApproved, t.DueDate })
            );

            var now = DateTime.Now; // Hoặc DateTime.UtcNow tùy thiết lập hệ thống của bạn

            return new ProjectTaskStatsDto
            {
                TotalTasks = tasksData.Count,
                InProgressTasks = tasksData.Count(t => t.Status == TaskStatus.InProgress),
                CompletedTasks = tasksData.Count(t => t.Status == TaskStatus.Completed),
                PendingTasks = tasksData.Count(t => !t.IsApproved),
                // Task quá hạn là task có hạn chót < hiện tại VÀ chưa hoàn thành
                OverdueTasks = tasksData.Count(t => t.DueDate.HasValue && t.DueDate.Value < now)
            };
        }

        public async Task<ListResultDto<UserLookupDto>> GetUserLookupAsync()
        {
            var users = await _userRepository.GetListAsync();

            return new ListResultDto<UserLookupDto>(
                ObjectMapper.Map<List<IdentityUser>, List<UserLookupDto>>(users)
            );
        }

        [Authorize]
        public async Task ApproveTaskAsync(Guid id)
        {
            var task = await Repository.GetAsync(id);
            var project = await _projectRepository.GetAsync(task.ProjectId);

            // Chỉ Admin hoặc PM mới được quyền Duyệt
            if (CurrentUser.IsInRole("admin") || project.ManagerId == CurrentUser.Id)
            {
                task.IsApproved = true;
                await Repository.UpdateAsync(task);
            }
            else
            {
                throw new UserFriendlyException(L["Only Project Managers or Admins have the authority to approve!"]);
            }
        }

        private async Task SendTaskNotificationAsync(List<Guid> assignedUserIds, string title, string message, Guid projectId)
        {
            var targetUrl = $"/projects/{projectId}"; // Đường dẫn trỏ tới trang Tasks (hoặc modal chi tiết nếu bạn có route riêng)
            var receivers = new HashSet<Guid>();

            // 1. Thêm những người được giao task vào danh sách nhận
            if (assignedUserIds != null)
            {
                foreach (var userId in assignedUserIds) receivers.Add(userId);
            }

            // 2. Tìm và thêm toàn bộ Admin vào danh sách nhận
            var admins = await _userManager.GetUsersInRoleAsync("admin");
            foreach (var admin in admins) receivers.Add(admin.Id);

            // 3. Loại bỏ ID của chính người đang thao tác (để họ không tự nhận thông báo của mình)
            receivers.Remove(CurrentUser.Id.GetValueOrDefault());

            // 4. Tạo list thông báo và lưu vào DB
            var notifications = receivers.Select(userId =>
                new AppNotification(GuidGenerator.Create(), userId, title, message, targetUrl, "Task")
            ).ToList();

            if (notifications.Any())
            {
                await _notificationRepository.InsertManyAsync(notifications, autoSave: true);

                foreach (var userId in receivers)
                {
                    await _localEventBus.PublishAsync(new NotificationEventData
                    {
                        ReceiverId = userId,
                        Title = title,
                        Message = message
                    });
                }
            }

        }
    }
}
