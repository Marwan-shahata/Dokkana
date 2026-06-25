// Mirrors Dokkana.BLL.DTOs.Notification.NotificationDTO
export interface NotificationDTO {
  id: number;
  title: string;
  message: string;
  type?: string | null;
  link?: string | null;
  isRead: boolean;
  isSponsored?: boolean;
  campaignId?: number | null;
  createdAt: string;
}

// Mirrors BroadcastNotificationDTO (admin only)
export interface BroadcastNotificationRequest {
  title: string;
  message: string;
  targetRole?: string | null;
  type?: string | null;
  link?: string | null;
}
