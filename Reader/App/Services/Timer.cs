using System.Timers;

namespace App.Services
{
    public interface ITimer
    {
        void Start();
        void Stop();

        Action? Callback { get; set; }
    }

    internal class Timer : ITimer
    {
        private readonly System.Timers.Timer InternalTimer;
        private readonly TimeSpan Interval;
        private bool IsExecuting = false;

        public Action? Callback { get; set; }

        public Timer(TimeSpan interval)
        {
            Interval = interval;
            InternalTimer = new System.Timers.Timer();
            InternalTimer.Interval = Interval.TotalMilliseconds;
            InternalTimer.Elapsed += Execute;
        }

        public void Start()
        {
            InternalTimer.Start();
        }

        public void Stop()
        {
            InternalTimer.Stop();
        }

        private void Execute(object? sender, ElapsedEventArgs e)
        {
            if (IsExecuting)
            {
                return;
            }

            IsExecuting = true;
            try
            {
                if (Callback != null)
                {
                    Callback();
                }
            }
            finally
            {
                IsExecuting = false;
            }
        }
    }
}