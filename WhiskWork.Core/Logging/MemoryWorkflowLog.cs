using System.Linq;
using System.Collections.Generic;

namespace WhiskWork.Core.Logging
{
    public class MemoryWorkflowLog : IWorkflowLog
    {
        readonly List<WorkItemLogEntry> _workItemLogEntries = new List<WorkItemLogEntry>();
        readonly List<WorkStepLogEntry> _workStepLogEntries = new List<WorkStepLogEntry>();

        public IEnumerable<WorkItemLogEntry> GetWorkItemLogEntries(string workItemId)
        {
            return _workItemLogEntries.Where(wile => wile.WorkItem.Id == workItemId);
        }

        public void AddLogEntry(WorkItemLogEntry logEntry)
        {
            _workItemLogEntries.Add(logEntry);
        }

        public void AddLogEntry(WorkStepLogEntry logEntry)
        {
            _workStepLogEntries.Add(logEntry);
        }
    }
}
