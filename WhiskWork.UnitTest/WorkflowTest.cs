using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Core;

namespace WhiskWork.UnitTest
{
    [TestClass]
    public class WorkflowTest
    {
        TestWorkflowRepository _workflowRepository;
        TestWorkItemRepository _workItemRepository;
        Workflow _wp;

        [TestInitialize]
        public void Init()
        {
            _workflowRepository = new TestWorkflowRepository();
            _workItemRepository = new TestWorkItemRepository();
            _wp = new Workflow(_workflowRepository, _workItemRepository);
        }

        [TestMethod, Ignore]
        public void ShouldSetCorrectOrdinalWhenMovingWorkItemBackAndForth()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 1, WorkStepType.End, "cr");

            throw new NotImplementedException();

        }
    }
}
