/**
 * Notifications API — thin wrapper around ZentavioCRM.Api's NotificationsController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { RelatedEntityType } from "@/services/activityService";

export interface Notification {
  id: string;
  message: string;
  relatedEntityType: RelatedEntityType | null;
  relatedEntityId: string | null;
  isRead: boolean;
  createdAtUtc: string;
}

const getRecent = () => callApi<Notification[]>(apiClient.get("/api/notifications"));

const getUnreadCount = () => callApi<number>(apiClient.get("/api/notifications/unread-count"));

const markAsRead = (id: string) => callApi<boolean>(apiClient.post(`/api/notifications/${id}/read`, {}));

const markAllAsRead = () => callApi<boolean>(apiClient.post("/api/notifications/read-all", {}));

export const notificationService = { getRecent, getUnreadCount, markAsRead, markAllAsRead };
