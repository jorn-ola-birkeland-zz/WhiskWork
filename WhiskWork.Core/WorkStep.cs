using System.Collections.Generic;

namespace WhiskWork.Core
{
    public class WorkStep
    {
        public WorkStep(string path, string parentPath, int ordinal, WorkStepType workStepType, string workItemClass) : this(path,parentPath,ordinal,workStepType,workItemClass,null)
        {
        }

        
        public WorkStep(string path, string parentPath, int ordinal, WorkStepType workStepType, string workItemClass, string title)
        {
            Path = path;
            ParentPath = parentPath;
            Type = workStepType;
            Ordinal = ordinal;
            WorkItemClass = workItemClass;
            Title = title;
        }

        public string Path { get; private set; }
        public string ParentPath { get; private set; }
        public int Ordinal { get; private set; }
        public WorkStepType Type { get; private set; }
        public string WorkItemClass { get; private set; }
        public string Title { get; private set; }
    }
}