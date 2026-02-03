using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
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
        }
    }
}
