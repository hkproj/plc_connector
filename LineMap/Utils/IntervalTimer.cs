using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LineMap.Utils
{
    public class IntervalTimer
    {

        public DateTime StartTime { get; private set; }

        public long Interval { get; private set;  }

        public bool OnEdge { get; set; } = true;

        long LastElapsedInterval { get; set; }

        public IntervalTimer(DateTime start_time, long interval)
        {
            Initialize(start_time, interval);
        }

        public IntervalTimer(long interval)
        {
            Initialize(DateTime.UtcNow, interval);
        }

        void Initialize(DateTime start_time, long interval)
        {
            if (interval <= 0)
                throw new ArgumentOutOfRangeException(nameof(interval), interval, "Interval must be positive");

            this.StartTime = start_time;
            this.Interval = interval;
        }

        public bool Elapsed
        {
            get
            {
                var interval_so_far = (long)(DateTime.UtcNow - StartTime).TotalSeconds;

                if (OnEdge && LastElapsedInterval == interval_so_far)
                    return false;

                bool elapsed = (interval_so_far > 0) && ((interval_so_far - LastElapsedInterval) >= Interval);

                if (elapsed)
                    LastElapsedInterval = interval_so_far;

                return elapsed;
            }
        }

        public void Reset()
        {
            LastElapsedInterval = 0;
        }

    }
}
