using System;
using Volo.Abp.Domain.Entities;

namespace TaskManagement.Projects;

public class ProjectMember : Entity
{
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }

    protected ProjectMember() { }

    public ProjectMember(Guid projectId, Guid userId)
    {
        ProjectId = projectId;
        UserId = userId;
    }

    public override object[] GetKeys()
    {
        return new object[] { ProjectId, UserId };
    }
}