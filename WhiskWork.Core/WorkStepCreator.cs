using System;
using System.Linq;

namespace WhiskWork.Core
{

    public class WorkStepCreator : WorkflowRepositoryInteraction
    {
        public WorkStepCreator(IWorkflowRepository workflowRepository) : base(workflowRepository)
        {
        }

        public void CreateWorkStep(WorkStep workStep)
        {
            if (workStep.ParentPath != WorkStep.Root.Path)
            {
                if (!WorkflowRepository.ExistsWorkStep(workStep.ParentPath))
                {
                    throw new InvalidOperationException("Parent does not exist");                    
                }
            }

            if(workStep.Type==WorkStepType.Transient)
            {
                throw new InvalidOperationException("Cannot create transient workstep");
            }

            WorkStep parent = null;
            if (workStep.ParentPath != WorkStep.Root.Path)
            {
                parent = WorkflowRepository.GetWorkStep(workStep.ParentPath);
            }

            if (workStep.WorkItemClass == null && parent!=null && parent.Type!=WorkStepType.Parallel)
            {
                workStep = workStep.UpdateWorkItemClass(parent.WorkItemClass);
            }

            if(parent!=null && parent.Type==WorkStepType.Parallel)
            {
                var leaf = WorkflowPath.GetLeafDirectory(workStep.Path);
                var workItemClass = parent.WorkItemClass + "-" + leaf;

                if(workStep.WorkItemClass==null)
                {
                    workStep = workStep.UpdateWorkItemClass(workItemClass);
                }
                else if(workStep.WorkItemClass!=workItemClass)
                {
                    throw new InvalidOperationException("Invalid work item class for child of parallel step. Expected '"+workItemClass+"' but was '"+workStep.WorkItemClass+"'");
                }
            }

            var siblings = WorkflowRepository.GetChildWorkSteps(workStep.ParentPath);
            var firstSibling = siblings.FirstOrDefault();

            if (workStep.WorkItemClass == null && firstSibling!=null)
            {
                workStep = workStep.UpdateWorkItemClass(firstSibling.WorkItemClass);
            }
            
            if(!workStep.Ordinal.HasValue)
            {
                var last = siblings.OrderBy(ws => ws.Ordinal).LastOrDefault();
                var ordinal = last == null ? 0 : last.Ordinal.Value + 1;
                workStep = workStep.UpdateOrdinal(ordinal);
            }

            if(siblings.Where(ws=>ws.Ordinal==workStep.Ordinal).Count()>0)
            {
                throw new InvalidOperationException("Cannot create workstep with same ordinal as sibling");
            }

            if(workStep.WorkItemClass==null)
            {
                throw new InvalidOperationException("Work item class missing and could not resolve one");
            }
            
            if(parent!=null && parent.Type==WorkStepType.Expand && parent.WorkItemClass==workStep.WorkItemClass)
            {
                throw new InvalidOperationException("Child of expand step cannot have same workitem class as parent");
            }
            
            if(parent!=null && parent.Type!=WorkStepType.Expand && parent.Type!=WorkStepType.Parallel && parent.WorkItemClass!=workStep.WorkItemClass)
            {
                throw new InvalidOperationException("Incompatible work item class. Should be same as parent");
            }

            if(firstSibling!=null && firstSibling.WorkItemClass!=workStep.WorkItemClass)
            {
                throw new InvalidOperationException("Incompatible work item class. Should be same as siblings");
                
            }

            WorkflowRepository.CreateWorkStep(workStep);

        }


    }
}
