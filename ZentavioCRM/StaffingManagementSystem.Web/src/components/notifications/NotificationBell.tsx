import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { notificationService, type Notification } from "@/services/notificationService";

const POLL_INTERVAL_MS = 30_000;

function relatedPath(notification: Notification): string | null {
  if (!notification.relatedEntityType || !notification.relatedEntityId) return null;
  switch (notification.relatedEntityType) {
    case "Lead":
      return `/leads/${notification.relatedEntityId}`;
    case "Opportunity":
      return `/opportunities/${notification.relatedEntityId}`;
    case "Customer":
      return `/customers/${notification.relatedEntityId}/edit`;
    case "Quotation":
      return `/quotations/${notification.relatedEntityId}`;
    case "SalesOrder":
      return `/sales-orders/${notification.relatedEntityId}`;
    default:
      return null;
  }
}

export function NotificationBell() {
  const navigate = useNavigate();
  const [unreadCount, setUnreadCount] = useState(0);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const refreshUnreadCount = async () => {
    const result = await notificationService.getUnreadCount();
    if (result.success && result.data != null) setUnreadCount(result.data);
  };

  useEffect(() => {
    refreshUnreadCount();
    const interval = setInterval(refreshUnreadCount, POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const togglePanel = async () => {
    const nextOpen = !isOpen;
    setIsOpen(nextOpen);
    if (nextOpen) {
      const result = await notificationService.getRecent();
      if (result.success && result.data) setNotifications(result.data);
    }
  };

  const handleNotificationClick = async (notification: Notification) => {
    if (!notification.isRead) {
      await notificationService.markAsRead(notification.id);
      setUnreadCount((c) => Math.max(0, c - 1));
    }
    setIsOpen(false);
    const path = relatedPath(notification);
    if (path) navigate(path);
  };

  const handleMarkAllRead = async () => {
    await notificationService.markAllAsRead();
    setNotifications((list) => list.map((n) => ({ ...n, isRead: true })));
    setUnreadCount(0);
  };

  return (
    <div className="position-relative" ref={containerRef}>
      <button type="button" className="btn btn-outline-secondary position-relative" onClick={togglePanel}>
        <i className="bi bi-bell" aria-hidden="true" />
        {unreadCount > 0 && (
          <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">
            {unreadCount > 9 ? "9+" : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div
          className="position-absolute end-0 mt-2"
          style={{
            width: 360,
            zIndex: 1050,
            maxHeight: 420,
            overflowY: "auto",
            background: "var(--itm-card-bg)",
            border: "1px solid var(--itm-border)",
            borderRadius: "var(--itm-radius-card)",
            boxShadow: "var(--itm-shadow-raised)",
          }}
        >
          <div
            className="d-flex justify-content-between align-items-center"
            style={{
              padding: "0.85rem 1.1rem",
              borderBottom: "1px solid var(--itm-border)",
              background: "linear-gradient(180deg, #fbfdff 0%, #f6f9fc 100%)",
            }}
          >
            <span className="fw-semibold" style={{ fontSize: "0.9rem" }}>
              <i className="bi bi-bell me-2 text-muted" aria-hidden="true" />
              Notifications
            </span>
            <button type="button" className="btn btn-sm btn-link p-0" onClick={handleMarkAllRead}>
              Mark all read
            </button>
          </div>
          <div className="list-group list-group-flush">
            {notifications.length === 0 && (
              <div className="p-3 text-muted small">No notifications yet.</div>
            )}
            {notifications.map((n) => (
              <button
                key={n.id}
                type="button"
                className="list-group-item list-group-item-action border-0 border-bottom"
                style={{ background: n.isRead ? "transparent" : "color-mix(in srgb, var(--itm-accent) 6%, white)" }}
                onClick={() => handleNotificationClick(n)}
              >
                <div className="small">{n.message}</div>
                <div className="text-muted" style={{ fontSize: "0.75rem" }}>
                  {new Date(n.createdAtUtc).toLocaleString()}
                </div>
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
