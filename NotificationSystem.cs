using System.Collections.Generic;
using UnityEngine;

namespace TheLongDriveSyncRadio
{
    public class NotificationSystem
    {
        private const float NOTIFICATION_DURATION = 5f;
        private readonly Queue<NotificationItem> notifications;
        private GUIStyle notificationStyle;

        public NotificationSystem()
        {
            notifications = new Queue<NotificationItem>();
        }

        public void Initialize()
        {
            notificationStyle = new GUIStyle
            {
                normal = { textColor = Color.white },
                fontSize = 16,
                alignment = TextAnchor.UpperRight,
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        public void ShowNotification(string message)
        {
            notifications.Enqueue(new NotificationItem 
            { 
                Message = message, 
                EndTime = Time.time + NOTIFICATION_DURATION 
            });
        }

        public void DrawNotifications()
        {
            while (notifications.Count > 0 && notifications.Peek().EndTime < Time.time)
            {
                notifications.Dequeue();
            }

            float yPosition = 10;
            foreach (var notification in notifications)
            {
                GUI.Label(new Rect(Screen.width - 310, yPosition, 300, 30), notification.Message, notificationStyle);
                yPosition += 35;
            }
        }

        private class NotificationItem
        {
            public string Message { get; set; }
            public float EndTime { get; set; }
        }
    }
}
