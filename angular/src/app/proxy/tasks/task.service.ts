import type { CreateUpdateTaskDto, GetTasksInput, ProjectTaskStatsDto, TaskDto, UserLookupDto } from './models';
import { RestService, Rest } from '@abp/ng.core';
import type { ListResultDto, PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TaskService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  approveTask = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'POST',
      url: `/api/task/${id}/approve`,
    },
    { apiName: this.apiName,...config });
  

  create = (input: CreateUpdateTaskDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'POST',
      url: '/api/task',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/task/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'GET',
      url: `/api/task/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: GetTasksInput, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<TaskDto>>({
      method: 'GET',
      url: '/api/task',
      params: { filterText: input.filterText, status: input.status, assignedUserId: input.assignedUserId, projectId: input.projectId, sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  getProjectStats = (projectId: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, ProjectTaskStatsDto>({
      method: 'GET',
      url: `/api/task/${projectId}/project-stats`,
    },
    { apiName: this.apiName,...config });
  

  getUserLookup = (config?: Partial<Rest.Config>) =>
    this.restService.request<any, ListResultDto<UserLookupDto>>({
      method: 'GET',
      url: '/api/task/user-lookup',
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateTaskDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, TaskDto>({
      method: 'PUT',
      url: `/api/task/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });
}