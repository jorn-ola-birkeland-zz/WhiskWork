using System;
using System.Collections.Specialized;
using System.Linq;
using Rhino.Mocks;
using WhiskWork.Core;

namespace WhiskWork.Test.Common
{
    public static class WorkItemRepositoryTestExtensions
    {
        public static void CreateWorkItem(this IWriteableWorkItemRepository workflow, string path, params string[] workItemIds)
        {
            Array.ForEach(workItemIds, workItemId => workflow.CreateWorkItem(WorkItem.New(workItemId, path)));
        }

        public static void CreateWorkItem(this IWriteableWorkItemRepository repository, WorkStep workStep, params string[] ids)
        {
            ids.ToList().ForEach(id => repository.CreateWorkItem(WorkItem.New(id, workStep.Path)));
        }


        public static void MoveWorkItem(this IWriteableWorkItemRepository workflow, string path, params string[] workItemIds)
        {
            Array.ForEach(workItemIds, workItemId => workflow.UpdateWorkItem(WorkItem.New(workItemId, path, new NameValueCollection())));
        }

    }

    public static class WorkStepRepositoryTestExtensions
    {
        public static WorkStep CreateWorkStep(this IWriteableWorkStepRepository repository, string path)
        {
            var step = WorkStep.New(path);
            repository.CreateWorkStep(step);
            return step;
        }

        public static WorkStep CreateWorkStep(this IWriteableWorkStepRepository repository, string path, WorkStepType type)
        {
            var step = WorkStep.New(path).UpdateType(type);
            repository.CreateWorkStep(step);
            return step;
        }

        public static WorkStep CreateWorkStep(this IWriteableWorkStepRepository repository, string path, int wipLimit)
        {
            var step = WorkStep.New(path).UpdateWipLimit(wipLimit);
            repository.CreateWorkStep(step);
            return step;
        }
    }


    public static class WorkProcessTestExtensions
    {

        public static void UpdateOrdinal(this Workflow workflow, string workItemId, int ordinal)
        {
            var wi = workflow.GetWorkItem(workItemId);

            workflow.UpdateWorkItem(wi.UpdateOrdinal(ordinal));
        }

        public static void MockTime(this Workflow workflow, MockRepository mocks, DateTime expectedTime)
        {
            var time = mocks.Stub<ITimeSource>();
            workflow.TimeSource = time;


            using (mocks.Record())
            {
                SetupResult.For(time.GetTime()).Return(expectedTime);
            }
        }


    }
}