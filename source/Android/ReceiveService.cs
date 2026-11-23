using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Lantoss.Android;

/// <summary>Keeps hopo running while it is in the background, so PCs can still find the phone and send to it. Its notification has a Stop action.</summary>
[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeDataSync)]
public sealed class ReceiveService : Service
{
    private const string Channel = "hopo-ready", StopAction = "hopo.stop";
    public static void Start(Context context)
    {
        Intent intent = new(context, typeof(ReceiveService));
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }
    public override IBinder? OnBind(Intent? intent) => null;
    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == StopAction)
        {
            if (HopoHost.Ready && HopoHost.Engine.Running)
                HopoHost.Engine.Stop();
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
            return StartCommandResult.NotSticky;
        }
        if (GetSystemService(NotificationService) is NotificationManager manager)
            manager.CreateNotificationChannel(new NotificationChannel(Channel, "Ready to receive", NotificationImportance.Low) { Description = "Shown while hopo can receive files" });
        Intent open = new(this, typeof(MainActivity));
        Intent stop = new(this, typeof(ReceiveService));
        stop.SetAction(StopAction);
        Notification notification = new Notification.Builder(this, Channel)
            .SetContentTitle("hopo is ready")
            .SetContentText("Your PCs can find this phone and send to it.")
            .SetSmallIcon(Resource.Drawable.icon)
            .SetOngoing(true)
            .SetContentIntent(PendingIntent.GetActivity(this, 0, open, PendingIntentFlags.Immutable))
            .AddAction(new Notification.Action.Builder(null, "Stop", PendingIntent.GetService(this, 1, stop, PendingIntentFlags.Immutable)).Build())
            .Build()!;
        if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
            StartForeground(1, notification, ForegroundService.TypeDataSync);
        else
            StartForeground(1, notification);
        return StartCommandResult.Sticky;
    }
    /// <summary>Android 15 limits background data sync; when the limit is reached hopo stops cleanly and starts again when opened.</summary>
    public override void OnTimeout(int startId, ForegroundService fgsType)
    {
        StopForeground(StopForegroundFlags.Remove);
        StopSelf();
    }
}
