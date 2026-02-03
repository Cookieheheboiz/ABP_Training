using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.Tasks
{
    public interface ITaskAppService : ICrudAppService<
        TaskDto,             
        Guid,                 
        GetTasksInput,         
        CreateUpdateTaskDto>    
    {
        Task<ListResultDto<UserLookupDto>> GetUserLookupAsync();
    }
}
