using System;
using System.Text;
using System.Text.RegularExpressions;

namespace WhiskWork.Core
{
    public static class ExpandedWorkStep
    {
        public static string GetTransientPath(WorkStep expandedWorkStep, WorkItem workItem)
        {
            return WorkflowPath.CombinePath(expandedWorkStep.Path, workItem.Id);
        }
    }

    public class WorkStep
    {
        private static readonly string _separator = new string(new [] {WorkflowPath.Separator});

        private readonly int? _ordinal;
        private readonly WorkStepType? _type;

        private WorkStep(string path, string parentPath)
        {
            Path = path;
            ParentPath = parentPath;
        }

        private WorkStep(string path, string parentPath, int? ordinal, WorkStepType? workStepType, string workItemClass, string title, int? wipLimit)
        {
            Path = path;
            ParentPath = parentPath;
            _type = workStepType;
            _ordinal = ordinal;
            WorkItemClass = workItemClass;
            Title = title;
            WipLimit = wipLimit;
        }

        public static WorkStep New(string path)
        {
            ThrowIfIllegalPath(path, "path");
            var parentPath = WorkflowPath.GetParentPath(path);
            ThrowIfIllegalPath(parentPath, "parentPath");

            return new WorkStep(path, parentPath);
        }

        public static WorkStep Root
        {
            get
            {
                return new WorkStep(_separator, null, 0, WorkStepType.Normal, null,null,null);
            }
        }


        public string Path { get; private set; }
        public string ParentPath { get; private set; }
        public int? Ordinal { get { return _ordinal; } }
        public WorkStepType Type
        {
            get { return _type.HasValue ? _type.Value : WorkStepType.Normal;  }
        }
        public string WorkItemClass { get; private set; }
        public string Title { get; private set; }
        public int? WipLimit { get; private set; }

        public override bool Equals(object obj)
        {
            if (!(obj is WorkStep))
            {
                return false;
            }

            var workStep = (WorkStep)obj;

            var result = true;

            result &= Path == workStep.Path;
            result &= ParentPath == workStep.ParentPath;
            result &= _ordinal == workStep._ordinal;
            result &= _type == workStep._type;
            result &= WorkItemClass==workStep.WorkItemClass;
            result &= Title==workStep.Title;
            result &= WipLimit == workStep.WipLimit;

            return result;
        }

        public override int GetHashCode()
        {
            var hc = Path != null ? Path.GetHashCode() : 2;
            hc ^= ParentPath != null ? ParentPath.GetHashCode() : 4;
            hc ^= _ordinal.HasValue ? _ordinal.Value.GetHashCode() :8 ;
            hc ^= _type.HasValue ? _type.Value.GetHashCode() : 16;
            hc ^= WorkItemClass != null ? WorkItemClass.GetHashCode() : 32;
            hc ^= Title != null ? Title.GetHashCode() : 64;
            hc ^= WipLimit.HasValue ? WipLimit.Value.GetHashCode() : 128;

            return hc;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Path={0},", Path);
            sb.AppendFormat("ParentPath={0},", ParentPath);
            sb.AppendFormat("Ordinal={0},", _ordinal);
            sb.AppendFormat("Type={0},", _type);
            sb.AppendFormat("WorkItemClass={0},", WorkItemClass);
            sb.AppendFormat("Title={0},", Title);
            sb.AppendFormat("WipLimit={0},", WipLimit);

            return sb.ToString();
        }

        private static void ThrowIfIllegalPath(string path, string paramName)
        {
            if (path == null)
            {
                throw new ArgumentNullException(paramName);
            }

            bool isValid = WorkflowPath.IsValidPath(path);

            if (!isValid)
            {
                throw new ArgumentException(paramName, "Path must start with '/' but was '" + path + "'");
            }
        }

        private static void ThrowIfIllegalWorkItemClass(string workItemClass, string paramName)
        {
            if (workItemClass == null)
            {
                throw new ArgumentNullException(paramName);
            }

            var regex = new Regex(@"^[0-9,a-z,A-Z,\-]+$");

            if (!regex.IsMatch(workItemClass))
            {
                throw new ArgumentException(paramName, "WorkItem class must only contain a-z,A-Z, 0-9, and - but was '" + workItemClass + "'");
            }

        }


        public WorkStep UpdateWorkItemClass(string workItemClass)
        {
            ThrowIfIllegalWorkItemClass(workItemClass, "workItemClass");
            return new WorkStep(Path,ParentPath,_ordinal,Type,workItemClass,Title,WipLimit);
        }

        public WorkStep UpdateWipLimit(int wipLimit)
        {
            return new WorkStep(Path, ParentPath, _ordinal, Type, WorkItemClass, Title, wipLimit);
        }

        public WorkStep UpdateOrdinal(int ordinal)
        {
             return new WorkStep(Path, ParentPath, ordinal, Type, WorkItemClass, Title, WipLimit);
        }

        public WorkStep UpdateType(WorkStepType workStepType)
        {
            return new WorkStep(Path, ParentPath, _ordinal, workStepType, WorkItemClass, Title, WipLimit);
        }

        public WorkStep UpdateTitle(string title)
        {
            return new WorkStep(Path, ParentPath, _ordinal, Type, WorkItemClass, title, WipLimit);
        }

        public WorkStep UpdatePath(string path)
        {
            var parentPath = WorkflowPath.GetParentPath(path);
            return new WorkStep(path, parentPath, _ordinal, Type, WorkItemClass, Title, WipLimit);
        }

        public WorkStep UpdateFrom(WorkStep workStep)
        {
            var returnStep = new WorkStep(Path, ParentPath, Ordinal, Type, WorkItemClass, Title, WipLimit);

            if (workStep._ordinal.HasValue)
            {
                returnStep = returnStep.UpdateOrdinal(workStep._ordinal.Value);
            }
            if (workStep.Title != null)
            {
                returnStep = returnStep.UpdateTitle(workStep.Title);
            }
            if(workStep._type.HasValue)
            {
                returnStep = returnStep.UpdateType(workStep._type.Value);
            }
            if(workStep.WorkItemClass!=null)
            {
                returnStep = returnStep.UpdateWorkItemClass(workStep.WorkItemClass);
            }
            if(workStep.WipLimit.HasValue)
            {
                returnStep = returnStep.UpdateWipLimit(workStep.WipLimit.Value);
            }

            return returnStep;
        }
    }
}