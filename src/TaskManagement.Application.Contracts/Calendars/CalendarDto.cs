using System;
using System.Collections.Generic;
using System.Text;
using TaskManagement.Tasks;

namespace TaskManagement.Calendars
{
    public class CalendarTaskDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime? DueDate { get; set; }
        public TaskStatus Status { get; set; }
        public bool IsApproved { get; set; }
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string? Description { get; set; }
        public List<string> AssignedUserNames { get; set; }
    }
}