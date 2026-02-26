using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace TaskManagement.Calendars
{
    public interface ICalendarAppService : IApplicationService
    {
        Task<List<CalendarTaskDto>> GetCalendarTasksAsync(GetCalendarTasksInput input);
    }
}