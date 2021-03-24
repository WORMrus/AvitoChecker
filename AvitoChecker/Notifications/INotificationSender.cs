namespace AvitoChecker.Notifications
{
    public interface INotificationSender
    {
        void SendNotification(AvitoListing listing);
    }
}
