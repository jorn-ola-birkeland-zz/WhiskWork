using System;
using System.Collections.Generic;

namespace WhiskWork.Core
{
    public class AdvancedWorkflowRepository : IWorkStepRepository
    {
        private readonly IWorkStepRepository _workStepRepository;

        public AdvancedWorkflowRepository(IWorkStepRepository workStepRepository)
        {
            _workStepRepository = workStepRepository;
        }

        public void DeleteWorkStepsRecursively(WorkStep step)
        {
            foreach (var workStep in _workStepRepository.GetChildWorkSteps(step.Path))
            {
                DeleteWorkStepsRecursively(workStep);
            }

            DeleteWorkStep(step.Path);
        }


        public void CreateWorkStep(WorkStep workStep)
        {
            _workStepRepository.CreateWorkStep(workStep);
        }

        public IEnumerable<WorkStep> GetChildWorkSteps(string path)
        {
            return _workStepRepository.GetChildWorkSteps(path);
        }

        public WorkStep GetWorkStep(string path)
        {
            return _workStepRepository.GetWorkStep(path);
        }

        public void DeleteWorkStep(string path)
        {
            _workStepRepository.DeleteWorkStep(path);
        }

        public bool ExistsWorkStep(string path)
        {
            return _workStepRepository.ExistsWorkStep(path);
        }
    }
}
