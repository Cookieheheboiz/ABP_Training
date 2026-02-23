using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Tasks
{
    public class TaskDto : AuditedEntityDto<Guid>
    {
        public Guid? CreatorId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public TaskStatus Status { get; set; }
        public bool IsApproved { get; set; }
        public DateTime? DueDate { get; set; }
        public List<Guid> AssignedUserIds { get; set; }
        public List<string> AssignedUserNames { get; set; }
        public Guid? ProjectManagerId { get; set; }
    }
}
