using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TaskManagement.Projects
{
    public interface IProjectAppService : ICrudAppService<
        ProjectDto,
        Guid,
        GetProjectsInput,
        CreateUpdateProjectDto>
    {
        Task<ListResultDto<UserLookupDto>> GetUserLookupAsync();
    }
}
