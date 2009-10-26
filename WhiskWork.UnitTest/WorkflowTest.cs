using System.Collections.Specialized;
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

        [TestMethod]
        public void ShouldSetCorrectOrdinalWhenCreatingWorkItem()
        {
            SetUpAndTestBasicOrdinal();
        }


        [TestMethod]
        public void ShouldSetCorrectOrdinalWhenMovingWorkItem()
        {
            SetUpAndTestBasicOrdinal();

            _wp.UpdateWorkItem("cr2", "/development", new NameValueCollection());
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());

            Assert.AreEqual(1, _workItemRepository.GetWorkItem("cr2").Ordinal);
            Assert.AreEqual(2, _workItemRepository.GetWorkItem("cr1").Ordinal);
        }

        [TestMethod]
        public void ShouldRenumOrdinalsInFromStepWhenMovingWorkItem()
        {
            SetUpAndTestBasicOrdinal();

            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());

            Assert.AreEqual(1, _workItemRepository.GetWorkItem("cr2").Ordinal);
            Assert.AreEqual(2, _workItemRepository.GetWorkItem("cr3").Ordinal);
        }

        [TestMethod]
        public void ShouldRenumOrdinalsWhenDeletingWorkItem()
        {
            SetUpAndTestBasicOrdinal();

            _wp.DeleteWorkItem("cr1");

            Assert.AreEqual(1, _workItemRepository.GetWorkItem("cr2").Ordinal);
            Assert.AreEqual(2, _workItemRepository.GetWorkItem("cr3").Ordinal);
        }

        private void SetUpAndTestBasicOrdinal()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 1, WorkStepType.End, "cr");

            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.CreateWorkItem("cr2", "/analysis");
            _wp.CreateWorkItem("cr3", "/analysis");

            Assert.AreEqual(1, _workItemRepository.GetWorkItem("cr1").Ordinal);
            Assert.AreEqual(2, _workItemRepository.GetWorkItem("cr2").Ordinal);
            Assert.AreEqual(3, _workItemRepository.GetWorkItem("cr3").Ordinal);
        }
    }
}
