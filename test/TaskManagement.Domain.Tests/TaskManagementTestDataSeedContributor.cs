using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TaskManagement.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

namespace TaskManagement
{
    public class TaskManagementTestDataSeedContributor : IDataSeedContributor, ITransientDependency
    {
        private readonly IRepository<TaskItem, Guid> _taskRepository;
        private readonly IIdentityUserRepository _userRepository;
        private readonly IGuidGenerator _guidGenerator;

        public TaskManagementTestDataSeedContributor(
            IRepository<TaskItem, Guid> taskRepository,
            IIdentityUserRepository userRepository,
            IGuidGenerator guidGenerator)
        {
            _taskRepository = taskRepository;
            _userRepository = userRepository;
            _guidGenerator = guidGenerator;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            var userId = _guidGenerator.Create();
            var user = new IdentityUser(userId, "user1", "user1@abp.io");

            if (await _userRepository.FindByNormalizedUserNameAsync("USER1") == null)
            {
                await _userRepository.InsertAsync(user);
            }

            await _taskRepository.InsertAsync(
                new TaskItem(
                    _guidGenerator.Create(),
                    "Test Task 1",
                    "Description for Test Task 1",
                    TaskManagement.Tasks.TaskStatus.New,
                    userId
                )
            );
        }
    }
}
