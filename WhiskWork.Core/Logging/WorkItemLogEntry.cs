using System;

namespace WhiskWork.Core.Logging
{
    public class WorkItemLogEntry
    {
        private WorkItemLogEntry()
        {
            Timestamp = DateTime.Now;
        }

        public WorkItem WorkItem { get; private set; }
        public LogOperationType LogOperation { get; private set; }
        public DateTime Timestamp { get; private set; }
        public WorkItem PreviousWorkItem { get; private set; }

        public static WorkItemLogEntry CreateEntry(WorkItem workItem)
        {
            return new WorkItemLogEntry {LogOperation = LogOperationType.Create, WorkItem = workItem};
        }


        public static WorkItemLogEntry DeleteEntry(WorkItem workItem)
        {
            return new WorkItemLogEntry { LogOperation = LogOperationType.Delete, WorkItem = workItem };
        }

        public static WorkItemLogEntry UpdateEntry(WorkItem newWorkItem, WorkItem oldWorkItem)
        {
            return new WorkItemLogEntry { LogOperation = LogOperationType.Update, WorkItem = newWorkItem, PreviousWorkItem = oldWorkItem };
        }

    }
}