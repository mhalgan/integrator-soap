using System;
using System.Timers;

namespace Integrator
{
    class SyncTimer : Timer
    {
        private int interval;

        public SyncTimer()
        {
            interval = 1;
            InitializeComponents();
        }

        public SyncTimer(int _interval)
        {
            interval = _interval;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            base.AutoReset = false;
            base.Interval = GetInterval(DateTime.Now);
            base.Elapsed += SyncTimer_Elapsed;
        }

        private void SyncTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            base.Interval = GetInterval(e.SignalTime);
        }

        private double GetInterval(DateTime now)
        {
            return (((interval - (now.Second % interval)) * 1000 - now.Millisecond) + 499);
        }
    }
}