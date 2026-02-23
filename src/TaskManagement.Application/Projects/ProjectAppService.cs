using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManagement.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;


namespace TaskManagement.Projects
{
    // Kế thừa CrudAppService để có sẵn các hàm CRUD cơ bản (Create, Update, Delete)
    public class ProjectAppService : CrudAppService<
            Project,
            ProjectDto,
            Guid,
            GetProjectsInput,
            CreateUpdateProjectDto>,
            IProjectAppService
    {
        private readonly IRepository<TaskItem, Guid> _taskRepository;
        private readonly IIdentityUserRepository _userRepository;

        public ProjectAppService(
            IRepository<Project, Guid> repository,
            IRepository<TaskItem, Guid> taskRepository,
            IIdentityUserRepository userRepository)
            : base(repository)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
        }

        public override async Task<ProjectDto> CreateAsync(CreateUpdateProjectDto input)
        {
            var entity = new Project(
                GuidGenerator.Create(),
                input.Name,
                input.Description,
                input.ManagerId
            );

            if (input.MemberIds != null && input.MemberIds.Any())
            {
                foreach (var userId in input.MemberIds)
                {
                    entity.AddMember(userId);
                }
            }

            await Repository.InsertAsync(entity, autoSave: true);
            var dto = ObjectMapper.Map<Project, ProjectDto>(entity);
            if (entity.Members != null)
            {
                dto.MemberIds = entity.Members.Select(x => x.UserId).ToList();
            }
            return dto;
        }

        public override async Task<ProjectDto> UpdateAsync(Guid id, CreateUpdateProjectDto input)
        {
            var queryable = await Repository.GetQueryableAsync();
            // Phải Include Members để Entity Framework biết mà xóa/sửa
            var project = await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Include(x => x.Members).Where(x => x.Id == id)
            );

            if (project == null) throw new EntityNotFoundException(typeof(Project), id);

            // Cập nhật thông tin cơ bản
            project.Name = input.Name;
            project.Description = input.Description;
            project.ManagerId = input.ManagerId;

            // Xóa danh sách thành viên cũ -> Thêm danh sách mới
            project.Members.Clear();
            if (input.MemberIds != null && input.MemberIds.Any())
            {
                foreach (var userId in input.MemberIds)
                {
                    project.AddMember(userId);
                }
            }

            await Repository.UpdateAsync(project, autoSave: true);
            var dto = ObjectMapper.Map<Project, ProjectDto>(project);
            if (project.Members != null)
            {
                dto.MemberIds = project.Members.Select(x => x.UserId).ToList();
            }
            return dto;
        }
        public override async Task<PagedResultDto<ProjectDto>> GetListAsync(GetProjectsInput input)
        {
            var currentUserId = CurrentUser.Id;
            var queryable = await Repository.GetQueryableAsync();
            queryable = queryable.Include(x => x.Members);

            if (!CurrentUser.IsInRole("admin"))
            {
                queryable = queryable.Where(x =>
                    x.ManagerId == currentUserId ||
                    x.Members.Any(m => m.UserId == currentUserId));
            }

            // Lọc theo tên hoặc mô tả
            if (!string.IsNullOrWhiteSpace(input.FilterText))
            {
                queryable = queryable.Where(x => x.Name.Contains(input.FilterText) || x.Description.Contains(input.FilterText));
            }

            // Đếm tổng
            var totalCount = await AsyncExecuter.CountAsync(queryable);

            // Sắp xếp
            if (string.IsNullOrWhiteSpace(input.Sorting))
            {
                input.Sorting = nameof(Project.CreationTime) + " DESC";
            }
            queryable = ApplySorting(queryable, input);
            queryable = ApplyPaging(queryable, input);

            // Lấy danh sách
            var projects = await AsyncExecuter.ToListAsync(queryable);

            // Map sang DTO
            var dtos = ObjectMapper.Map<List<Project>, List<ProjectDto>>(projects);

            var projectIds = dtos.Select(x => x.Id).ToList();
            if (projectIds.Any())
            {
                // Lấy tất cả Task của các project này
                //var tasks = await _taskRepository.GetListAsync(x => projectIds.Contains(x.ProjectId) && x.IsApproved);
                // thanh tiến độ sẽ được cập nhật với tất cả những task do user đề xuất tạo
                var tasks = await _taskRepository.GetListAsync(x => projectIds.Contains(x.ProjectId));
                
                // Lấy Manager
                var managerIds = dtos.Select(x => x.ManagerId).Distinct().ToList();
                var managers = await _userRepository.GetListByIdsAsync(managerIds);
                var managerDict = managers.ToDictionary(x => x.Id, x => x.UserName);

                foreach (var dto in dtos)
                {
                    var originalProject = projects.First(p => p.Id == dto.Id);
                    if (originalProject.Members != null)
                    {
                        dto.MemberIds = originalProject.Members.Select(m => m.UserId).ToList();
                    }

                    if (managerDict.ContainsKey(dto.ManagerId))
                    {
                        dto.ManagerName = managerDict[dto.ManagerId];
                    }

                    var approvedTasks = tasks.Where(t => t.ProjectId == dto.Id).ToList();
                    var totalApproved = approvedTasks.Count;

                    if (totalApproved > 0)
                    {
                        // Tiến độ = (Số task Hoàn thành / Tổng số task ĐÃ DUYỆT) * 100
                        var completedTasks = approvedTasks.Count(t => t.Status == TaskManagement.Tasks.TaskStatus.Completed);
                        dto.Progress = (int)((double)completedTasks / totalApproved * 100);
                    }
                    else
                    {
                        dto.Progress = 0;
                    }
                }
            }

            return new PagedResultDto<ProjectDto>(totalCount, dtos);
        }

        public override async Task<ProjectDto> GetAsync(Guid id)
        {
            var queryable = await Repository.GetQueryableAsync();
            var project = await AsyncExecuter.FirstOrDefaultAsync(
                queryable.Include(x => x.Members).Where(x => x.Id == id)
            );

            var dto = ObjectMapper.Map<Project, ProjectDto>(project);
            if (project.Members != null)
            {
                dto.MemberIds = project.Members.Select(x => x.UserId).ToList();
            }
            return dto;
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