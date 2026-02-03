using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Xunit;
using TaskManagement.Tasks;

using TaskManagement;

namespace TaskManagement.Tasks
{
    public class TaskAppService_Tests : TaskManagementApplicationTestBase<TaskManagementApplicationTestModule>
    {
        private readonly ITaskAppService _taskAppService;

        public TaskAppService_Tests()
        {
            _taskAppService = GetRequiredService<ITaskAppService>();
        }

        [Fact]
        public async Task Should_Get_List_Of_Tasks_With_User_Name()
        {
            var result = await _taskAppService.GetListAsync(
                new GetTasksInput()
            );

            result.TotalCount.ShouldBeGreaterThan(0);

            var task = result.Items.FirstOrDefault(t => t.Title == "Test Task 1");
            task.ShouldNotBeNull();

            task.AssignedUserName.ShouldBe("user1");
        }

        [Fact]
        public async Task Should_Create_A_New_Task()
        {
            var input = new CreateUpdateTaskDto
            {
                Title = "New Task created inside Test",
                Description = "Testing create...",
                Status = TaskStatus.New
            };

            var result = await _taskAppService.CreateAsync(input);

            result.Id.ShouldNotBe(Guid.Empty);
            result.Title.ShouldBe("New Task created inside Test");
        }
    }
}