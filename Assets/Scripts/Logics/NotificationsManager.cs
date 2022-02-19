using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Notifications.Android;
using UnityEngine;

public static class NotificationsManager
{
    public static void Init()
    {
        var channel = new AndroidNotificationChannel()
        {
            Id = "channel_0",
            Name = "Default Channel",
            Importance = Importance.Default,
            Description = "Generic notifications",
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    }

    public static void SendNotification(string desc, int minutes, int id)
    {
        var notification = new AndroidNotification();
        notification.Title = "Ultimate Spaceship Arena";
        notification.Text = LangResolver.instance.GetLocalizedString(desc);
        notification.FireTime = DateTime.Now.AddMinutes(minutes);
        AndroidNotificationCenter.CancelNotification(id);
        AndroidNotificationCenter.SendNotificationWithExplicitID(notification, "channel_0", id);
    }
}
