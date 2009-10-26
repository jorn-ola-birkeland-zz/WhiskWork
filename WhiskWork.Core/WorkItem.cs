using System;
using System.Collections.Generic;

namespace WhiskWork.Core
{
    public class WorkItem
    {
        private WorkItem(string id, string path, IEnumerable<string> workItemClasses, WorkItemStatus status, string parentId, int ordinal)
        {
            Id = id;
            Path = path;
            Classes = workItemClasses;
            Status = status;
            ParentId = parentId;
            Ordinal = ordinal;
        }

        public static WorkItem New(string id, string path, IEnumerable<string> workItemClasses)
        {
            return new WorkItem(id, path, workItemClasses, WorkItemStatus.Normal, null,0);
        }

        public string Id { get; private set; }
        public string Path { get; private set; }
        public IEnumerable<string> Classes { get; private set; }
        public WorkItemStatus Status  { get; private set; }
        public string ParentId { get; private set; }
        public int Ordinal { get; private set; }

        public WorkItem MoveTo(WorkStep step)
        {
            return new WorkItem(Id,step.Path,Classes,Status,ParentId, Ordinal);
        }

        public WorkItem UpdateStatus(WorkItemStatus status)
        {
            return new WorkItem(Id, Path, Classes, status, ParentId, Ordinal);
        }

        public WorkItem AddClass(string workItemClass)
        {
            var newClasses = new List<string>(Classes) {workItemClass};

            return new WorkItem(Id, Path, newClasses, Status, ParentId, Ordinal);
        }

        public WorkItem CreateChildItem(string id)
        {
            return new WorkItem(id, Path, Classes, Status, Id, Ordinal);
        }

        public WorkItem UpdateParent(WorkItem parentItem)
        {
            return new WorkItem(Id, Path, Classes, Status, parentItem.Id, Ordinal);
        }

        public WorkItem UpdateOrdinal(int ordinal)
        {
            return new WorkItem(Id, Path, Classes, Status, ParentId, ordinal);
        }

        public WorkItem RemoveClass(string workItemClass)
        {
            var newClasses = new List<string>(Classes);
            newClasses.Remove(workItemClass);

            return new WorkItem(Id, Path, newClasses, Status, ParentId, Ordinal);
        }
    }
}