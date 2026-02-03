using Volo.Abp.Modularity;
using TaskManagement.EntityFrameworkCore;
namespace TaskManagement;

[DependsOn(
    typeof(TaskManagementApplicationModule),
    typeof(TaskManagementEntityFrameworkCoreTestModule)
)]
public class TaskManagementApplicationTestModule : AbpModule
{

}
