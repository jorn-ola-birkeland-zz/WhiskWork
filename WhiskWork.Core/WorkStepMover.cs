using System;
using System.Linq;

namespace WhiskWork.Core
{
    public class WorkStepMover
    {
        private readonly IWorkflowRepository _workflowRepository;
        private readonly ITimeSource _timeSource;

        public WorkStepMover(IWorkflowRepository workflowRepository, ITimeSource timeSource)
        {
            _workflowRepository = workflowRepository;
            _timeSource = timeSource;
        }

        public void MoveWorkStep(WorkStep stepToMove, WorkStep toStep)
        {
            if(stepToMove.WorkItemClass!=toStep.WorkItemClass)
            {
                throw new InvalidOperationException("Cannot move work step. Work item classes are not compatible");
            }

            var siblings = _workflowRepository.GetChildWorkSteps(toStep.Path);
            if(siblings.Where(ws=>ws.Ordinal==stepToMove.Ordinal).Count()>0)
            {
                throw new InvalidOperationException("Cannot move work step. Conflicting ordinals");
            }

            if(_workflowRepository.IsWithinTransientStep(stepToMove))
            {
                throw new InvalidOperationException("Cannot move transient work step or step within transient work step");
            }

            var commonRoot = WorkflowPath.FindCommonRoot(stepToMove.Path, toStep.Path);

            foreach (var path in WorkflowPath.GetPathsBetween(commonRoot,stepToMove.Path))
            {
                if(path==commonRoot || path==stepToMove.Path)
                {
                    continue;
                }

                if(_workflowRepository.GetWorkStep(path).Type==WorkStepType.Expand)
                {
                    throw new InvalidOperationException("Cannot move work step within expand step outside expand step");
                }

                if (_workflowRepository.GetWorkStep(path).Type == WorkStepType.Parallel)
                {
                    throw new InvalidOperationException("Cannot move work step within expand step outside parallel step");
                }

            }

            var wipLimitChecker = new WipLimitChecker(_workflowRepository);

            if(!wipLimitChecker.CanAcceptWorkStep(toStep,stepToMove))
            {
                throw new InvalidOperationException("Cannot move work step when WIP limit of new ancestors are violated");
            }




            MoveWorkStepRecursively(stepToMove, toStep);
        }

        private void MoveWorkStepRecursively(WorkStep stepToMove, WorkStep toStep)
        {
            var leafDirectory = WorkflowPath.GetLeafDirectory(stepToMove.Path);

            var newPath = WorkflowPath.CombinePath(toStep.Path, leafDirectory);

            var newStep = stepToMove.UpdatePath(newPath);
            _workflowRepository.CreateWorkStep(newStep);

            foreach (var workItem in _workflowRepository.GetWorkItems(stepToMove.Path))
            {
                _workflowRepository.UpdateWorkItem(workItem.MoveTo(newStep,_timeSource.GetTime()));
            }

            foreach (var childWorkStep in _workflowRepository.GetChildWorkSteps(stepToMove.Path))
            {
                MoveWorkStep(childWorkStep, newStep);
            }

            _workflowRepository.DeleteWorkStep(stepToMove.Path);
        }
    }
}