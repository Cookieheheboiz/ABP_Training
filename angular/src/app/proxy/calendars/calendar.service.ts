import type { CalendarTaskDto, GetCalendarTasksInput } from './models';
import { RestService, Rest } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CalendarService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  getCalendarTasks = (input: GetCalendarTasksInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CalendarTaskDto[]>({
      method: 'GET',
      url: '/api/app/calendar/calendar-tasks',
      params: { projectId: input.projectId, startDate: input.startDate, endDate: input.endDate },
    },
    { apiName: this.apiName,...config });
}