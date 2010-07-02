#region

using System;

#endregion

namespace WhiskWork.Core.Exception
{
    public class WipLimitViolationException : InvalidOperationException
    {
        public WipLimitViolationException(WorkItem workItem, WorkStep toWorkStep)
            : base(
                string.Format("WIP limit violated. Cannot move work item '{0}' to work step '{1}'", 
                    workItem.Id, toWorkStep.Path))
        {
        }
    }
}