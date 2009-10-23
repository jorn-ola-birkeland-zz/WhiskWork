using System;
using System.Collections.Generic;
using System.Linq;
using WhiskWork.Core;

namespace WhiskWork.UnitTest
{
    public class TestWorkflowRepository : IWorkflowRepository
    {
        private readonly Dictionary<string, WorkStep> _workSteps = new Dictionary<string, WorkStep>();

        public void Add(string path, string parentPath, int index, WorkStepType stepType, string workItemClass)
        {
            _workSteps.Add(path, new WorkStep(path,parentPath,index,stepType, workItemClass, null));
        }

        public void Add(string path, string parentPath, int index, WorkStepType stepType, string workItemClass, string title)
        {
            _workSteps.Add(path, new WorkStep(path, parentPath, index, stepType, workItemClass, title));
        }


        public IEnumerable<WorkStep> GetChildWorkSteps(string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }

            return _workSteps.Values.Where(step => step.ParentPath == path).ToList();
        }

        public WorkStep GetWorkStep(string path)
        {
            return _workSteps[path];
        }

        public void DeleteWorkStep(string path)
        {
            _workSteps.Remove(path);
        }

        public void CreateWorkStep(WorkStep workStep)
        {
            _workSteps.Add(workStep.Path, workStep);
        }

        public bool ExistsWorkStep(string path)
        {
            return _workSteps.ContainsKey(path);
        }
    }
}