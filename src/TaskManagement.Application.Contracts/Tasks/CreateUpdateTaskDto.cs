using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Tasks
{
    public class CreateUpdateTaskDto
    {
        [Required]
        [StringLength(128)]
        public string Title { get; set; }

        [StringLength(2048)]
        public string? Description { get; set; }

        [Required]
        public TaskStatus Status { get; set; } = TaskStatus.New;
        public bool IsApproved { get; set; } = false;

        public DateTime? DueDate { get; set; }
        public List<Guid> AssignedUserIds { get; set; } = new List<Guid>();

        [Required]
        public Guid ProjectId { get; set; }
    }
}
