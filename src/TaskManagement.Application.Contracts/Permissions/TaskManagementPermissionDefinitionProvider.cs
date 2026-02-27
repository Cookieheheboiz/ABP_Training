using TaskManagement.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace TaskManagement.Permissions;

public class TaskManagementPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(TaskManagementPermissions.GroupName, L("Permission:TaskManagement"));

        var tasksPermission = myGroup.AddPermission(TaskManagementPermissions.Tasks.Default, L("Permission:Tasks"));
        tasksPermission.AddChild(TaskManagementPermissions.Tasks.Create, L("Permission:Tasks.Create"));
        tasksPermission.AddChild(TaskManagementPermissions.Tasks.Update, L("Permission:Tasks.Update"));
        tasksPermission.AddChild(TaskManagementPermissions.Tasks.Delete, L("Permission:Tasks.Delete"));

        var projectsPermission = myGroup.AddPermission(TaskManagementPermissions.Projects.Default, L("Permission:Projects"));
        projectsPermission.AddChild(TaskManagementPermissions.Projects.Create, L("Permission:Projects.Create"));
        projectsPermission.AddChild(TaskManagementPermissions.Projects.Update, L("Permission:Projects.Update"));
        projectsPermission.AddChild(TaskManagementPermissions.Projects.Delete, L("Permission:Projects.Delete"));

        myGroup.AddPermission(TaskManagementPermissions.Calendars.Default, L("Permission:Calendars"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<TaskManagementResource>(name);
    }
}
