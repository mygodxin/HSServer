using Core;
using Core.Timers;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSServer
{
    public class TimerTest
    {
        public TimerTest()
        {
        }

        public static void Test()
        {
            QuartzTimer.Delay<DelayTimer>(1, TimeSpan.FromSeconds(3));
        }
    }

    public class DelayTimer : ITimerHandler
    {
        public void HandleTimer(long actor, object param)
        {
            Logger.Error("Test DelayTimer ");
        }
    }
}
