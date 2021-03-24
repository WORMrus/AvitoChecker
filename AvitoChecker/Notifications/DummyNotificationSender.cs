namespace AvitoChecker.Notifications
{
    class DummyNotificationSender : INotificationSender
    {
        public void SendNotification(AvitoListing listing)
        {
            return;
        }
    }
}
