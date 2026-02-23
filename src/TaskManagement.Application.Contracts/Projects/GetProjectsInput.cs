using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Projects
{
    public class GetProjectsInput : PagedAndSortedResultRequestDto
    {
        public string? FilterText { get; set; }
    }
}