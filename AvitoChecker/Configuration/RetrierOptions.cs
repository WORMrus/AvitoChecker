using AvitoChecker.Retriers;

namespace AvitoChecker.Configuration
{
    class RetrierOptions
    {
        public int Attempts { get; set; }
        public int Delay { get; set; }
        public bool LogRetriesAfterFirstAttempt { get; set; }
        public ActionOnFailure OnFailure { get; set; }
    }
}
