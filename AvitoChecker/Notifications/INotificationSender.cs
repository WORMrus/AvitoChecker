using AvitoChecker.ListingUtilities;

namespace AvitoChecker.Notifications
{
    public interface INotificationSender
    {
        void SendNotification(Listing listing);
    }
}
