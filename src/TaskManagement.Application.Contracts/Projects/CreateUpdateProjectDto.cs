using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Projects
{
    public class CreateUpdateProjectDto
    {
        [Required]
        [StringLength(128)]
        public string Name { get; set; }

        [StringLength(2048)]
        public string? Description { get; set; }

        [Required]
        public Guid ManagerId { get; set; }
        public List<Guid> MemberIds { get; set; } = new List<Guid>();
    }
}