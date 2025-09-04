using Proto;
using Quartz;

namespace Core.Timers
{
    public interface ITimerHandler
    {
        void HandleTimer(IActor actor, object param);
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
