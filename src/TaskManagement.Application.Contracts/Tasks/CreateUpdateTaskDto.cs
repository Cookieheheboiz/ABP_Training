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
        public string Description { get; set; }

        [Required]
        public TaskStatus Status { get; set; } = TaskStatus.New;

        public Guid? AssignedUserId { get; set; }
    }
}
