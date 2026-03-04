import type { TaskStatus } from '../tasks/task-status.enum';

export interface CalendarTaskDto {
  id?: string;
  title?: string;
  dueDate?: string | null;
  status?: TaskStatus;
  isApproved?: boolean;
  projectId?: string;
  projectName?: string;
  description?: string | null;
  assignedUserNames?: string[];
}

export interface GetCalendarTasksInput {
  projectId?: string | null;
  startDate?: string;
  endDate?: string;
}
