using WhiskWork.Core.Logging;

namespace WhiskWork.Core
{
    public interface IWorkflowLog
    {
        void AddLogEntry(WorkItemLogEntry logEntry);
        void AddLogEntry(WorkStepLogEntry logEntry);
    }
}