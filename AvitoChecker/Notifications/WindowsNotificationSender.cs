using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;

namespace AvitoChecker.Notifications
{
    public class WindowsNotificationSender : INotificationSender
    {

        public WindowsNotificationSender()
        {
            ToastNotificationManagerCompat.OnActivated += (toastArgs) =>
            {
                //based on https://stackoverflow.com/a/43232486
                string url = toastArgs.Argument;
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            };
        }
        public void SendNotification(AvitoListing listing)
        {
            new ToastContentBuilder().AddText("New listing found")
                                     .AddText(listing.Name)
                                     .AddText($"Price: {listing.Price}")
                                     .AddButton(new ToastButton("Open in a browser", listing.Link))
                                     .Show();


        }
    }
}
