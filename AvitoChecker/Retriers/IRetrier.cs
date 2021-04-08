using System;
using System.Threading;
using System.Threading.Tasks;

namespace AvitoChecker.Retriers
{
    public interface IRetrier
    {
        ActionOnFailure OnFailure { get; set; }

        T Attempt<T>(Func<T> func);
        Task<T> AttemptAsync<T>(Func<Task<T>> func, CancellationToken stoppingToken);

        void Attempt(Func<Task> action);
        Task AttemptAsync(Func<Task> action, CancellationToken stoppingToken);
    }

    public enum ActionOnFailure
    {
        Throw,
        ReturnDefault
    }
}
