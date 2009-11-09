using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WhiskWork.Core
{
    public class WorkStep
    {
        public const string Separator = "/";

        public WorkStep(string path, string parentPath, int ordinal, WorkStepType workStepType, string workItemClass) : this(path,parentPath,ordinal,workStepType,workItemClass,null, true)
        {
        }


        public WorkStep(string path, string parentPath, int ordinal, WorkStepType workStepType, string workItemClass, string title)
            : this(path, parentPath, ordinal, workStepType, workItemClass, title, true)
        {
        }

        public WorkStep(string path, string parentPath, int ordinal, WorkStepType workStepType, string workItemClass, string title, bool validate)
        {
            if(validate)
            {
                ThrowIfIllegalPath(path, "path");
                ThrowIfIllegalPath(path, "parentPath");
                ThrowIfParentPathIsNotProperSubPathOfPath(path, parentPath);
            }

            Path = path;
            ParentPath = parentPath;
            Type = workStepType;
            Ordinal = ordinal;
            WorkItemClass = workItemClass;
            Title = title;
        }


        public static WorkStep Root
        {
            get
            {
                return new WorkStep(Separator, null, 0, WorkStepType.Normal, null,null,false);
            }
        }

        public static string CombinePath(string path1, string path2)
        {
            path1 = path1.EndsWith("/") ? path1.Remove(path1.Length - 1, 1) : path1;
            path2 = path2.StartsWith("/") ? path2.Remove(0, 1) : path2;

            var result = path1 + "/" + path2;
            return result;
        }


        private static void ThrowIfParentPathIsNotProperSubPathOfPath(string path, string parentPath)
        {
            var separator = parentPath == Root.Path ? string.Empty : Separator;

            var regex = new Regex(parentPath + separator + @"[a-z,A-Z,0-9,\-]+$");
            if (!regex.IsMatch(path))
            {
                throw new ArgumentException(string.Format("parent path '{0}' is not sub path of path '{1}'", parentPath, path));
            }
        }

        private static void ThrowIfIllegalPath(string path, string paramName)
        {
            if(path==null)
            {
                throw new ArgumentNullException(paramName);
            }

            var regex = new Regex(@"^(\/)$|^(\/[0-9,a-z,A-Z,\-]+)+$");

            if(!regex.IsMatch(path))
            {
                throw new ArgumentException(paramName, "Path must start with '/' but was '"+path+"'");
            }
        }

        public string Path { get; private set; }
        public string ParentPath { get; private set; }
        public int Ordinal { get; private set; }
        public WorkStepType Type { get; private set; }
        public string WorkItemClass { get; private set; }
        public string Title { get; private set; }

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
            result &= Ordinal == workStep.Ordinal;
            result &= Type== workStep.Type;
            result &= WorkItemClass==workStep.WorkItemClass;
            result &= Title==workStep.Title;

            return result;
        }

        public override int GetHashCode()
        {
            var hc = Path != null ? Path.GetHashCode() : 2;
            hc ^= ParentPath != null ? ParentPath.GetHashCode() : 4;
            hc ^= Ordinal.GetHashCode();
            hc ^= Type.GetHashCode();
            hc ^= WorkItemClass != null ? WorkItemClass.GetHashCode() : 8;
            hc ^= Title != null ? Title.GetHashCode() : 16;

            return hc;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Path={0},", Path);
            sb.AppendFormat("ParentPath={0},", ParentPath);
            sb.AppendFormat("Ordinal={0},", Ordinal);
            sb.AppendFormat("Type={0},", Type);
            sb.AppendFormat("WorkItemClass={0},", WorkItemClass);
            sb.AppendFormat("Title={0}", Title);

            return sb.ToString();
        }

    }
}