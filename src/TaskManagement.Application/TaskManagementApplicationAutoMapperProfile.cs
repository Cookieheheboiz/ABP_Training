using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaskManagement.Projects;
using TaskManagement.Tasks;
using Volo.Abp.Identity;

namespace TaskManagement
{
    public class TaskManagementApplicationAutoMapperProfile : Profile
    {
        public TaskManagementApplicationAutoMapperProfile()
        {
            // Cấu hình map 2 chiều
            CreateMap<TaskItem, TaskDto>();
            CreateMap<CreateUpdateTaskDto, TaskItem>();
            CreateMap<IdentityUser, UserLookupDto>();
            CreateMap<Project, ProjectDto>()
                .ForMember(dest => dest.MemberIds, opt => opt.MapFrom(src => src.Members.Select(m => m.UserId).ToList()));
            CreateMap<CreateUpdateProjectDto, Project>();
        }
    }
}
