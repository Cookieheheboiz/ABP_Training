using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks
{
    public class UserLookupDto : EntityDto<Guid>
    {
        public string UserName { get; set; }
    }
}
