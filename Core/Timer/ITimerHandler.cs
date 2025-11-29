using Quartz;
using System.Threading.Tasks;

namespace Core.Timers
{
    public interface ITimerHandler
    {
        void HandleTimer(long actor, object param);
    }

    public abstract class NotHotfixTimerHandler : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            var param = context.JobDetail.JobDataMap.Get(QuartzTimer.PARAM_KEY);
            return HandleTimer(param);
        }

        protected abstract Task HandleTimer(object param);
    }
}
