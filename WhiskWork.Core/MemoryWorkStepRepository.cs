using System;
using System.Collections.Generic;
using System.Linq;

namespace WhiskWork.Core
{
    public class MemoryWorkStepRepository : IWorkStepRepository
    {
        private readonly Dictionary<string, WorkStep> _workSteps = new Dictionary<string, WorkStep>();

        public void Add(WorkStep workStep)
        {
            _workSteps.Add(workStep.Path, workStep);
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
            ThrowIfNotExists(path);

            return _workSteps[path];
        }

        private void ThrowIfNotExists(string path)
        {
            if(!_workSteps.ContainsKey(path))
            {
                throw new ArgumentException("Workstep not found: '"+path+"'");
            }
        }

        public void DeleteWorkStep(string path)
        {
            _workSteps.Remove(path);
        }

        public void UpdateWorkStep(WorkStep workStep)
        {
            ThrowIfNotExists(workStep.Path);

            _workSteps[workStep.Path] = workStep;
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