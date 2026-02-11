using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Permissions;
using TaskManagement.Services.Base;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;


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
        public TaskAppService(ITaskItemRepository repository, IIdentityUserRepository userRepository)
             : base(repository)
        {
            _userRepository = userRepository;
            _taskItemRepository = repository;

            GetPolicyName = "TaskManagement.Tasks";
            GetListPolicyName = "TaskManagement.Tasks";
            CreatePolicyName = "TaskManagement.Tasks.Create";
            UpdatePolicyName = "TaskManagement.Tasks.Update";
            DeletePolicyName = "TaskManagement.Tasks.Delete";
        }

        public override async Task<TaskDto> UpdateAsync(Guid id, CreateUpdateTaskDto input)
        {
            // Trước khi thực hiện nội dung hàm bên dưới thì kiểm tra quyền hiện tại có hợp lệ không
            await CheckPolicyAsync(UpdatePolicyName);

            // Lấy Task từ Database lên
            var task = await Repository.GetAsync(id);

            // Kiểm tra: Nếu không phải Admin VÀ không phải người tạo -> Báo lỗi
            var isCreator = task.CreatorId == CurrentUser.Id;
            var isAssignee = task.AssignedUserId == CurrentUser.Id; // Kiểm tra là người được giao việc
            var isManager = await AuthorizationService.IsGrantedAsync(DeletePolicyName);

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
                // Nếu là Admin hoặc Creator thì được sửa
                ObjectMapper.Map(input, task);
                await Repository.UpdateAsync(task, autoSave: true);
            }

            return ObjectMapper.Map<TaskItem, TaskDto>(task);
        }

        public override async Task DeleteAsync(Guid id)
        {
            // Trước khi thực hiện nội dung hàm bên dưới thì kiểm tra quyền hiện tại có hợp lệ không
            await CheckPolicyAsync(DeletePolicyName);

            var task = await Repository.GetAsync(id);

            var isCreator = task.CreatorId == CurrentUser.Id;
            var isManager = await AuthorizationService.IsGrantedAsync(DeletePolicyName);

            // Assignee KHÔNG ĐƯỢC PHÉP XÓA (trừ khi đó cũng là Creator)
            if (!isCreator && !isManager)
            {
                throw new UserFriendlyException(L["TaskDeletionForbidden"]);
            }

            await _taskItemRepository.DeleteTaskAsync(id);
        }
        public override async Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input)
        {
            // Trước khi thực hiện nội dung hàm bên dưới thì kiểm tra quyền hiện tại có hợp lệ không
            await CheckPolicyAsync(GetListPolicyName);   

            var queryable = await Repository.GetQueryableAsync();

            var filter = input.FilterText?.ToLower().Trim();
            queryable = queryable
                .WhereIf(!string.IsNullOrWhiteSpace(filter), x =>
                    x.Title.ToLower().Contains(filter) || (x.Description != null && x.Description.ToLower().Contains(filter))
                )
                .WhereIf(input.Status.HasValue, x => x.Status == input.Status)
                .WhereIf(input.AssignedUserId.HasValue, x => x.AssignedUserId == input.AssignedUserId);

            var totalCount = await AsyncExecuter.CountAsync(queryable);

            if (string.IsNullOrWhiteSpace(input.Sorting))
            {
                input.Sorting = nameof(TaskItem.CreationTime) + " DESC";
            }

            queryable = ApplySorting(queryable, input);
            queryable = ApplyPaging(queryable, input);

            var tasks = await AsyncExecuter.ToListAsync(queryable);

            var taskDtos = ObjectMapper.Map<System.Collections.Generic.List<TaskItem>, System.Collections.Generic.List<TaskDto>>(tasks);

            var userIds = tasks
            .Where(t => t.AssignedUserId.HasValue)
            .Select(t => t.AssignedUserId.Value)
            .Distinct()
            .ToList();

            if (userIds.Any())
            {
                var userList = await _userRepository.GetListByIdsAsync(userIds);

                // Tạo từ điển để tra cứu cho nhanh: Key=Id, Value=UserName
                var userDictionary = userList.ToDictionary(u => u.Id, u => u.UserName);

                // Lặp qua danh sách TaskDto và điền tên vào
                foreach (var dto in taskDtos)
                {
                    if (dto.AssignedUserId.HasValue && userDictionary.ContainsKey(dto.AssignedUserId.Value))
                    {
                        dto.AssignedUserName = userDictionary[dto.AssignedUserId.Value];
                    }
                }
            }

            return new PagedResultDto<TaskDto>(
                totalCount,
                taskDtos
            );
        }

        public async Task<ListResultDto<UserLookupDto>> GetUserLookupAsync()
        {
            var users = await _userRepository.GetListAsync();

            return new ListResultDto<UserLookupDto>(
                ObjectMapper.Map<List<IdentityUser>, List<UserLookupDto>>(users)
            );
        }
    }
}
