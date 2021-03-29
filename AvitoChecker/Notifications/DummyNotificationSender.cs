using AvitoChecker.ListingUtilities;

namespace AvitoChecker.Notifications
{
    class DummyNotificationSender : INotificationSender
    {
        public void SendNotification(Listing listing)
        {
            return;
        }
    }
}
