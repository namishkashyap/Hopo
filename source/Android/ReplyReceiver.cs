using Android.Content;

namespace Lantoss.Android;

/// <summary>The Decline, Accept and Always accept buttons on an incoming-files notification. They answer without opening hopo.</summary>
[BroadcastReceiver(Exported = false)]
public sealed class ReplyReceiver : BroadcastReceiver
{
    public const string Request = "hopo.request", Choice = "hopo.choice";
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent == null)
            return;
        AndroidApp.Answer(intent.GetIntExtra(Request, -1), intent.GetIntExtra(Choice, 0));
    }
}
