
export interface NotificationDto {
  id?: string;
  title?: string;
  message?: string;
  targetUrl?: string;
  isRead?: boolean;
  notificationType?: string;
  creationTime?: string;
}
