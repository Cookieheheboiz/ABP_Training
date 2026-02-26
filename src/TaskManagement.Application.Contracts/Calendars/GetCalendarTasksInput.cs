using System;
using System.Collections.Generic;
using System.Text;

namespace TaskManagement.Calendars
{
    public class GetCalendarTasksInput
    {
        public Guid? ProjectId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}