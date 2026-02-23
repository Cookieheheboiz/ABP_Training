import type { TaskStatus } from './task-status.enum';
import type { AuditedEntityDto, EntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CreateUpdateTaskDto {
  title: string;
  description?: string | null;
  status: TaskStatus;
  isApproved?: boolean;
  dueDate?: string | null;
  assignedUserIds?: string[];
  projectId: string;
}

export interface GetTasksInput extends PagedAndSortedResultRequestDto {
  filterText?: string | null;
  status?: TaskStatus | null;
  assignedUserId?: string | null;
  projectId?: string | null;
}

export interface TaskDto extends AuditedEntityDto<string> {
  creatorId?: string | null;
  title?: string;
  description?: string;
  projectId?: string | null;
  projectName?: string;
  status?: TaskStatus;
  isApproved?: boolean;
  dueDate?: string | null;
  assignedUserIds?: string[];
  assignedUserNames?: string[];
  projectManagerId?: string | null;
}

export interface UserLookupDto extends EntityDto<string> {
  userName?: string;
}
