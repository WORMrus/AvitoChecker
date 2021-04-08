using AvitoChecker.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AvitoChecker.Retriers
{
    class Retrier : IRetrier
    {
        protected Exception _lastException;
        protected ILogger _logger;
        protected Action<int, bool> _loggingAction;

        public int Attempts { get; set; }
        public int Delay { get; set; }
        public ActionOnFailure OnFailure { get; set; }

        public Retrier(IOptions<RetrierOptions> options, ILogger<Retrier> logger)
        {
            var opts = options.Value;

            _logger = logger;
            Attempts = opts.Attempts;
            Delay = opts.Delay;
            OnFailure = opts.OnFailure;

            if (opts.LogRetriesAfterFirstAttempt)
            {
                _loggingAction = (int i, bool firstAttemptFailed) =>
                {
                    if (firstAttemptFailed)
                    {
                        _logger.LogWarning(_lastException, $"Action failed for {i} times");
                    }
                };
            }
        }

        public async Task<T> AttemptAsync<T>(Func<Task<T>> func, CancellationToken stoppingToken)
        {
            bool firstAttemptFailed = false;
            for (int i = 0; !stoppingToken.IsCancellationRequested && i < Attempts; i++)
            {
                _loggingAction?.Invoke(i, firstAttemptFailed);
                try
                {
                    return await func();
                }
                catch (Exception e)
                {
                    firstAttemptFailed = true;
                    _lastException = e;
                    await Task.Delay(Delay, stoppingToken);
                }
            }
            return Fail<T>();
        }

        public T Attempt<T>(Func<T> func)
        {
            bool firstAttemptFailed = false;
            for (int i = 0; i < Attempts; i++)
            {
                _loggingAction?.Invoke(i, firstAttemptFailed);
                try
                {
                    return func();
                }
                catch (Exception e)
                {
                    firstAttemptFailed = true;
                    _lastException = e;
                    Task.Delay(Delay).Wait();
                }
            }
            return Fail<T>();
        }

        public async void Attempt(Func<Task> action)
        {
            bool firstAttemptFailed = false;
            for (int i = 0; i < Attempts; i++)
            {
                _loggingAction?.Invoke(i, firstAttemptFailed);
                try
                {
                    await action();
                }
                catch (Exception e)
                {
                    firstAttemptFailed = true;
                    _lastException = e;
                    Task.Delay(Delay).Wait();
                }
            }
            Fail();
        }

        public async Task AttemptAsync(Func<Task> action, CancellationToken stoppingToken)
        {
            bool firstAttemptFailed = false;
            for (int i = 0; !stoppingToken.IsCancellationRequested && i < Attempts; i++)
            {
                _loggingAction?.Invoke(i, firstAttemptFailed);
                try
                {
                    firstAttemptFailed = true;
                    await action();
                    return;
                }
                catch (Exception e)
                {
                    firstAttemptFailed = true;
                    _lastException = e;
                    await Task.Delay(Delay, stoppingToken);
                }
            }
            Fail();
        }

        protected T Fail<T>()
        {
            return OnFailure switch
            {
                ActionOnFailure.ReturnDefault => default,
                _ => throw new RetrierException(
                    $"Failed to execute the provided function for {Attempts} times. See the inner Exception for the last caught one",
                    _lastException),
            };
        }

        protected void Fail()
        {
            switch (OnFailure)
            {
                case ActionOnFailure.ReturnDefault:
                    return;
                case ActionOnFailure.Throw:
                default:
                    throw new RetrierException(
                        $"Failed to execute the provided function for {Attempts} times. See the inner Exception for the last caught one",
                        _lastException);
            }
        }

    }

    [Serializable]
    public class RetrierException : Exception
    {
        public RetrierException() : base() { }
        public RetrierException(string message) : base(message) { }
        public RetrierException(string message, Exception inner) : base(message, inner) { }
        protected RetrierException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
