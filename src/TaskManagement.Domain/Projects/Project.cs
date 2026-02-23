using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Volo.Abp.Domain.Entities.Auditing;

namespace TaskManagement.Projects;

public class Project : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; }
    public string? Description { get; set; }

    public Guid ManagerId { get; set; }

    // Danh sách các thành viên tham gia dự án
    public ICollection<ProjectMember> Members { get; set; }

    protected Project()
    {
        Members = new Collection<ProjectMember>();
    }

    public Project(Guid id, string name, string description, Guid managerId)
        : base(id)
    {
        Name = name;
        Description = description;
        ManagerId = managerId;
        Members = new Collection<ProjectMember>();

        // Mặc định PM cũng là một thành viên của dự án
        AddMember(managerId);
    }

    // Hàm Helper để thêm thành viên
    public void AddMember(Guid userId)
    {
        if (!Members.Any(m => m.UserId == userId))
        {
            Members.Add(new ProjectMember(Id, userId));
        }
    }

    // Hàm Helper để xóa thành viên
    public void RemoveMember(Guid userId)
    {
        var member = Members.FirstOrDefault(m => m.UserId == userId);
        if (member != null && userId != ManagerId) // Không được xóa PM khỏi danh sách thành viên
        {
            Members.Remove(member);
        }
    }
}