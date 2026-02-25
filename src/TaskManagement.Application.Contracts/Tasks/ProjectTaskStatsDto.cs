using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Tasks
{
    public class ProjectTaskStatsDto
    {
        public int TotalTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int PendingTasks { get; set; }
    }
}