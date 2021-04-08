namespace AvitoChecker.Configuration
{
    public class WorkerOptions
    {
        public int ListingPollingInterval { get; set; }
        public WorkerBehaviorOnException OnException { get; set; }
        public enum WorkerBehaviorOnException
        {
            StopApp,
            StopWorker,
            Continue
        }
    }
}
