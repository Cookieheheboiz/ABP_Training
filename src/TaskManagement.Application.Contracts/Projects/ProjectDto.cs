using System;
using System.Collections.Generic;
using System.Text;
using Volo.Abp.Application.Dtos;

namespace TaskManagement.Projects
{
    public class ProjectDto : AuditedEntityDto<Guid>
    {
        public string Name { get; set; }
        public string? Description { get; set; }

        public Guid ManagerId { get; set; }
        public string ManagerName { get; set; } // Map từ ManagerId sang tên để hiển thị
        public List<Guid> MemberIds { get; set; } = new List<Guid>();
        // Không lưu trong DB, mà tính toán lúc lấy dữ liệu
        public int Progress { get; set; }
    }
}