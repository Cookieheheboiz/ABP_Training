using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Domain.Entities.Auditing;
using TaskManagement.Projects;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagement.Tasks
{
    public class TaskItem : FullAuditedAggregateRoot<Guid>
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public TaskStatus Status { get; set; }
        public Guid ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public virtual Project Project { get; set; }

        public bool IsApproved { get; set; }
        public DateTime? DueDate { get; set; }

        public ICollection<TaskAssignee> Assignees { get; set; }

        

        protected TaskItem()
        {
            Assignees = new List<TaskAssignee>();
        }

        public TaskItem(Guid id, string title, string description, TaskStatus status, Guid projectId, bool isApproved, DateTime? dueDate = null)
            : base(id)
        {
            Title = title;
            Description = description;
            Status = status;
            ProjectId = projectId;
            IsApproved = isApproved;
            DueDate = dueDate;

            Assignees = new Collection<TaskAssignee>();
        }

        /// Add thêm người vào task
        public void AddAssignee(Guid userId)
        {
            if (!Assignees.Any(x => x.UserId == userId))
            {
                Assignees.Add(new TaskAssignee(Id, userId));
            }
        }

        /// Xóa một người khỏi task
        public void RemoveAssignee(Guid userId)
        {
            var assignee = Assignees.FirstOrDefault(x => x.UserId == userId);
            if (assignee != null)
            {
                Assignees.Remove(assignee);
            }
        }
    }
}
