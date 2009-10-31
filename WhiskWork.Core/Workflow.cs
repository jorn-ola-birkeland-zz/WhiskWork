using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WhiskWork.Core
{
    public class Workflow
    {
        private readonly AdvancedWorkflowRepository _workflowRepository;
        private readonly IWorkItemRepository _workItemRepository;
        private readonly WorkStepQuery _workStepQuery;
        private readonly WorkItemQuery _workItemQuery;

        public Workflow(IWorkflowRepository workflowRepository, IWorkItemRepository workItemRepository)
        {
            _workflowRepository = new AdvancedWorkflowRepository(workflowRepository);
            _workItemRepository = workItemRepository;
            _workStepQuery = new WorkStepQuery(_workflowRepository);
            _workItemQuery = new WorkItemQuery(_workflowRepository, _workItemRepository);
        }

        public IEnumerable<WorkItem> GetWorkItems(string path)
        {
            return _workItemRepository.GetWorkItems(path).Where(wi => wi.Status != WorkItemStatus.ParallelLocked);
        }

        public void CreateWorkItem(string id, string path)
        {
            CreateWorkItem(id,path,new NameValueCollection());
        }

        public void CreateWorkItem(string id, string path, NameValueCollection properties)
        {
            CreateWorkItem(WorkItem.New(id,path,properties));
        }

        public void CreateWorkItem(WorkItem newWorkItem)
        {
            if(!IsValidId(newWorkItem.Id))
            {
                throw new ArgumentException("Id can only consist of letters, numbers and hyphen");
            }

            var leafStep = _workStepQuery.GetLeafStep(newWorkItem.Path);

            if(leafStep.Type!=WorkStepType.Begin)
            {
                throw new InvalidOperationException("Can only create work items in begin step");
            }

            var classes = _workStepQuery.GetWorkItemClasses(leafStep);

            newWorkItem = newWorkItem.MoveTo(leafStep).ReplacesClasses(classes);

            WorkStep transientStep;
            if(_workStepQuery.IsWithinTransientStep(leafStep, out transientStep))
            {
                var workItems = _workItemRepository.GetWorkItems(transientStep.Path);
                Debug.Assert(workItems.Count()==1);

                var parentItem = workItems.ElementAt(0);
                _workItemRepository.UpdateWorkItem(parentItem.UpdateStatus(WorkItemStatus.ExpandLocked));

                newWorkItem = newWorkItem.MoveTo(leafStep).UpdateParent(parentItem);

                foreach (var workItemClass in newWorkItem.Classes)
                {
                    foreach (var rootClass in WorkItemClass.FindRootClasses(workItemClass))
                    {
                        newWorkItem = newWorkItem.AddClass(rootClass);
                    }
                }
            }
            else if(_workStepQuery.IsWithinExpandStep(leafStep))
            {
                throw new InvalidOperationException("Cannot create item directly under expand step");
            }

            newWorkItem = newWorkItem.UpdateOrdinal(_workItemQuery.GetNextOrdinal(newWorkItem));

            _workItemRepository.CreateWorkItem(newWorkItem);
        }

        private static bool IsValidId(string workItemId)
        {
            var regex = new Regex("^[\\-,a-z,A-Z,0-9]*$");
            return regex.IsMatch(workItemId);
        }

        public WorkItem UpdateWorkItem(string id, string path, NameValueCollection properties)
        {
            WorkItem workItem;
            if(!_workItemQuery.TryLocateWorkItem(id, out workItem))
            {
                throw new ArgumentException("Work item was not found");
            }

            WorkStep leafStep = _workStepQuery.GetLeafStep(path);

            WorkItem result=null;

            if (properties.Count > 0)
            {
                workItem = UpdateWorkItem(workItem, properties);
            }

            if(workItem.Path!=leafStep.Path)
            {
                result = MoveWorkItem(workItem, leafStep);
            }

            return result;
        }

        private WorkItem UpdateWorkItem(WorkItem workItem, NameValueCollection properties)
        {
            workItem = workItem.UpdateProperties(properties);
            _workItemRepository.UpdateWorkItem(workItem);
            return workItem;
        }

        private WorkItem MoveWorkItem(WorkItem workItem, WorkStep toStep)
        {
            var stepToMoveTo = toStep;

            var workItemToMove = workItem;

            if (_workItemQuery.IsParallelLockedWorkItem(workItem))
            {
                throw new InvalidOperationException("Work item is locked for parallel work");
            }

            if (_workItemQuery.IsExpandLocked(workItem))
            {
                throw new InvalidOperationException("Item is expandlocked and cannot be moved");
            }

            ThrowIfMovingFromTransientStepToParallelStep(workItem, toStep);
            ThrowIfMovingChildOfParalleledWorkItemToExpandStep(workItem, toStep);

            WorkStep parallelStep;
            if (_workStepQuery.IsWithinParallelStep(toStep, out parallelStep))
            {
                if (!_workItemQuery.IsChildOfParallelledWorkItem(workItem))
                {
                    string idToMove = ParallelStepHelper.GetParallelId(workItem.Id, parallelStep, toStep);
                    workItemToMove = MoveAndLockAndSplitForParallelism(workItem, parallelStep).First(wi => wi.Id==idToMove);
                }
                else
                {
                    workItemToMove = workItem; 
                }
            }

            if (_workStepQuery.IsExpandStep(toStep))
            {
                stepToMoveTo = CreateTransientWorkSteps(workItemToMove, stepToMoveTo);
                workItemToMove = workItemToMove.AddClass(stepToMoveTo.WorkItemClass);
            }


            if (!_workStepQuery.IsValidWorkStepForWorkItem(workItemToMove, stepToMoveTo))
            {
                throw new InvalidOperationException("Invalid step for work item");
            }

            if (_workItemQuery.IsChildOfParallelledWorkItem(workItemToMove))
            {
                if (IsMergeable(workItemToMove, toStep))
                {
                    workItemToMove = MergeParallelWorkItems(workItemToMove);
                }
            }

            workItemToMove = CleanUpIfInTransientStep(workItemToMove);

            var movedWorkItem = Move(workItemToMove, stepToMoveTo);

            if (_workItemQuery.IsChildOfExpandedWorkItem(movedWorkItem))
            {
                TryUpdatingExpandLockOnParent(movedWorkItem);
            }

            return movedWorkItem;
        }


        private void ThrowIfMovingFromTransientStepToParallelStep(WorkItem workItem, WorkStep toStep)
        {
            WorkStep transientStep;

            var isInTransientStep = _workStepQuery.IsInTransientStep(workItem, out transientStep);

            WorkStep parallelStepRoot;
            var isWithinParallelStep = _workStepQuery.IsWithinParallelStep(toStep, out parallelStepRoot);

            if(isInTransientStep && isWithinParallelStep)
            {
                throw new InvalidOperationException("Cannot move directly from transient step to parallelstep");
            }
        }

        private void ThrowIfMovingChildOfParalleledWorkItemToExpandStep(WorkItem workItem, WorkStep toStep)
        {
            if(!_workItemQuery.IsChildOfParallelledWorkItem(workItem))
            {
                return;
            }

            if(_workStepQuery.IsExpandStep(toStep))
            {
                throw new InvalidOperationException("Cannot move paralleled work item to expand step");
            }
        }

        private WorkItem CleanUpIfInTransientStep(WorkItem workItemToMove)
        {
            WorkStep transientStep;
            if (_workStepQuery.IsInTransientStep(workItemToMove, out transientStep))
            {
                DeleteChildWorkItems(workItemToMove);
                _workflowRepository.DeleteWorkStepsRecursively(transientStep);
                workItemToMove = workItemToMove.RemoveClass(transientStep.WorkItemClass);
            }
            return workItemToMove;
        }

        private void DeleteChildWorkItems(WorkItem workItem)
        {
            foreach (var childWorkItem in _workItemRepository.GetChildWorkItems(workItem.Id))
            {
                DeleteWorkItem(childWorkItem.Id);
            }
        }

        private WorkItem Move(WorkItem workItemToMove, WorkStep stepToMoveTo)
        {
            var fromStepPath = workItemToMove.Path;

            var movedWorkItem = workItemToMove.MoveTo(stepToMoveTo);

            var ordinal = _workItemQuery.GetNextOrdinal(movedWorkItem);
            movedWorkItem = movedWorkItem.UpdateOrdinal(ordinal);

            _workItemRepository.UpdateWorkItem(movedWorkItem);

            RenumOrdinals(fromStepPath);

            return movedWorkItem;
        }

        private void RenumOrdinals(string path)
        {
            var ordinal = 1;
            foreach (var workItem in _workItemRepository.GetWorkItems(path).OrderBy(wi=>wi.Ordinal))
            {
                _workItemRepository.UpdateWorkItem(workItem.UpdateOrdinal(ordinal++));
            }
        }

        private void TryUpdatingExpandLockOnParent(WorkItem item)
        {
            WorkItem parent = _workItemRepository.GetWorkItem(item.ParentId);

            if (_workItemRepository.GetChildWorkItems(parent.Id).All(_workItemQuery.IsDone))
            {
                parent = parent.UpdateStatus(WorkItemStatus.Normal);
            }
            else
            {
                parent = parent.UpdateStatus(WorkItemStatus.ExpandLocked);
            }

            _workItemRepository.UpdateWorkItem(parent);
        }



        private WorkStep CreateTransientWorkSteps(WorkItem item, WorkStep expandStep)
        {
            Debug.Assert(expandStep.Type==WorkStepType.Expand);

            var transientRootPath = expandStep.Path+"/"+item.Id;

            CreateTransientWorkStepsRecursively(transientRootPath,expandStep, item.Id);

            var workItemClass = WorkItemClass.Combine(expandStep.WorkItemClass, item.Id);
            var transientWorkStep = new WorkStep(transientRootPath, expandStep.Path, expandStep.Ordinal, WorkStepType.Transient, workItemClass, expandStep.Title);
            _workflowRepository.CreateWorkStep(transientWorkStep);

            return transientWorkStep;
        }

        private void CreateTransientWorkStepsRecursively(string transientRootPath, WorkStep rootStep, string workItemId)
        {
            var subSteps = _workflowRepository.GetChildWorkSteps(rootStep.Path).Where(ws=>ws.Type!=WorkStepType.Transient);
            foreach (var childStep in subSteps)
            {
                var offset = childStep.Path.Remove(0, rootStep.Path.Length);

                var childTransientPath = transientRootPath + offset;

                var workItemClass = WorkItemClass.Combine(childStep.WorkItemClass,workItemId);
                _workflowRepository.CreateWorkStep(new WorkStep(childTransientPath, transientRootPath, childStep.Ordinal, childStep.Type, workItemClass, childStep.Title));

                CreateTransientWorkStepsRecursively(childTransientPath, childStep, workItemId);
            }
        }


        private WorkItem MergeParallelWorkItems(WorkItem item)
        {
            WorkItem unlockedParent = _workItemRepository.GetWorkItem(item.ParentId).UpdateStatus(WorkItemStatus.Normal);
            _workItemRepository.UpdateWorkItem(unlockedParent);

            foreach (WorkItem child in _workItemRepository.GetChildWorkItems(item.ParentId).ToList())
            {
                _workItemRepository.DeleteWorkItem(child);
            }

            return unlockedParent;
        }

        private bool IsMergeable(WorkItem item, WorkStep toStep)
        {
            bool isMergeable = true;
            foreach (WorkItem child in _workItemRepository.GetChildWorkItems(item.ParentId).Where(wi=>wi.Id!=item.Id))
            {
                isMergeable &= child.Path == toStep.Path;
            }
            return isMergeable;
        }


        private IEnumerable<WorkItem> MoveAndLockAndSplitForParallelism(WorkItem item, WorkStep parallelRootStep)
        {
            var lockedAndMovedItem = item.MoveTo(parallelRootStep).UpdateStatus(WorkItemStatus.ParallelLocked);
            _workItemRepository.UpdateWorkItem(lockedAndMovedItem);

            var helper = new ParallelStepHelper(_workflowRepository);

            var splitWorkItems = helper.SplitForParallelism(item, parallelRootStep);

            foreach (var splitWorkItem in splitWorkItems)
            {
                _workItemRepository.CreateWorkItem(splitWorkItem);
            }

            return splitWorkItems;
        }


        public void DeleteWorkItem(string id)
        {
            var workItem = _workItemRepository.GetWorkItem(id);

            ThrowInvalidOperationExceptionIfParentIsParallelLocked(workItem);

            DeleteWorkItemRecursively(workItem);
        }

        private void ThrowInvalidOperationExceptionIfParentIsParallelLocked(WorkItem workItem)
        {
            if(workItem.ParentId!=null)
            {
                var parent = _workItemRepository.GetWorkItem(workItem.ParentId);
                if(parent.Status == WorkItemStatus.ParallelLocked)
                {
                    throw new InvalidOperationException("Cannot delete workitem which is child of paralleled workitem");
                }
            }
        }

        private void DeleteWorkItemRecursively(WorkItem workItem)
        {
            var childWorkItems = _workItemRepository.GetChildWorkItems(workItem.Id);

            if(childWorkItems.Count()>0)
            {
                foreach (var childWorkItem in childWorkItems)
                {
                    DeleteWorkItemRecursively(childWorkItem);
                }
            }


            _workItemRepository.DeleteWorkItem(workItem);
            RenumOrdinals(workItem.Path);
            CleanUpIfInTransientStep(workItem);
        }

        public bool ExistsWorkItem(string workItemId)
        {
            return _workItemRepository.ExistsWorkItem(workItemId);
        }

        public WorkItem GetWorkItem(string id)
        {
            return _workItemRepository.GetWorkItem(id);
        }

        public bool ExistsWorkStep(string path)
        {
            return _workflowRepository.ExistsWorkStep(path);
        }

        public WorkStep GetWorkStep(string path)
        {
            return _workflowRepository.GetWorkStep(path);
        }

        public void CreateWorkStep(WorkStep workStep)
        {
            _workflowRepository.CreateWorkStep(workStep);
        }
    }
}