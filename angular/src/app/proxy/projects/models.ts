import type { AuditedEntityDto, PagedAndSortedResultRequestDto } from '@abp/ng.core';

export interface CreateUpdateProjectDto {
  name: string;
  description?: string | null;
  managerId: string;
  memberIds?: string[];
}

export interface GetProjectsInput extends PagedAndSortedResultRequestDto {
  filterText?: string | null;
}

export interface ProjectDto extends AuditedEntityDto<string> {
  name?: string;
  description?: string | null;
  managerId?: string;
  managerName?: string;
  memberIds?: string[];
  progress?: number;
}
