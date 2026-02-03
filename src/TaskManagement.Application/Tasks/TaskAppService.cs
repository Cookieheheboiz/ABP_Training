using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;


namespace TaskManagement.Tasks
{
    public class TaskAppService : CrudAppService<
            TaskItem,
            TaskDto,
            Guid,
            GetTasksInput,
            CreateUpdateTaskDto>,
            ITaskAppService
    {
        private readonly IIdentityUserRepository _userRepository;
        public TaskAppService(IRepository<TaskItem, Guid> repository, IIdentityUserRepository userRepository)
            : base(repository)
        {
            _userRepository = userRepository;
            GetPolicyName = TaskManagementPermissions.Tasks.Default;
            GetListPolicyName = TaskManagementPermissions.Tasks.Default;
            CreatePolicyName = TaskManagementPermissions.Tasks.Create;
            UpdatePolicyName = TaskManagementPermissions.Tasks.Update;
            DeletePolicyName = TaskManagementPermissions.Tasks.Delete;
        }

        public override async Task<PagedResultDto<TaskDto>> GetListAsync(GetTasksInput input)
        {
            var queryable = await Repository.GetQueryableAsync();

            queryable = queryable
                .WhereIf(!string.IsNullOrWhiteSpace(input.FilterText), x => x.Title.Contains(input.FilterText))
                .WhereIf(input.Status.HasValue, x => x.Status == input.Status)
                .WhereIf(input.AssignedUserId.HasValue, x => x.AssignedUserId == input.AssignedUserId);

            var totalCount = await AsyncExecuter.CountAsync(queryable);

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
