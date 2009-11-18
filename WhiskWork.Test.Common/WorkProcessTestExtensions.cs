using System;
using System.Collections.Specialized;
using WhiskWork.Core;

namespace WhiskWork.Test.Common
{
    public static class WorkProcessTestExtensions
    {
        public static void Create(this IWorkflow workflow, string path, params string[] workItemIds)
        {
            Array.ForEach(workItemIds, workItemId => workflow.CreateWorkItem(WorkItem.New(workItemId, path)));
        }

        public static void Move(this IWorkflow workflow, string path, params string[] workItemIds)
        {
            Array.ForEach(workItemIds, workItemId => workflow.UpdateWorkItem(WorkItem.New(workItemId, path, new NameValueCollection())));
        }

        public static void UpdateOrdinal(this IWorkflow workflow, string workItemId, int ordinal)
        {
            var wi = workflow.GetWorkItem(workItemId);

            workflow.UpdateWorkItem(wi.UpdateOrdinal(ordinal));
        }

    }
}