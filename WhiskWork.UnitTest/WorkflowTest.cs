using System;
using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Core;

namespace WhiskWork.UnitTest
{
    [TestClass]
    public class WorkflowTest
    {
        MemoryWorkflowRepository _workflowRepository;
        MemoryWorkItemRepository _workItemRepository;
        Workflow _wp;

        [TestInitialize]
        public void Init()
        {
            _workflowRepository = new MemoryWorkflowRepository();
            _workItemRepository = new MemoryWorkItemRepository();
            _wp = new Workflow(_workflowRepository, _workItemRepository);
        }

        [TestMethod]
        public void ShouldCreateWorkItemInBeginStep()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr");

            _wp.CreateWorkItem("cr1", "/analysis");
            Assert.AreEqual("/analysis", _workItemRepository.GetWorkItem("cr1").Path);
        }

        [TestMethod]
        public void ShouldNotCreateWorkItemInNormalStep()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr");

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.CreateWorkItem("cr1", "/development"));
        }


        [TestMethod]
        public void ShouldNotCreateWorkItemWithSlashInId()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");

            AssertUtils.AssertThrows<ArgumentException>(
                () => _wp.CreateWorkItem("cr/1", "/analysis"));
            AssertUtils.AssertThrows<ArgumentException>(
                () => _wp.CreateWorkItem("/cr1", "/analysis"));
            AssertUtils.AssertThrows<ArgumentException>(
                () => _wp.CreateWorkItem("cr1/", "/analysis"));
        }

        [TestMethod]
        public void ShouldNotCreateWorkItemWithDotInId()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");

            AssertUtils.AssertThrows<ArgumentException>(
                () => _wp.CreateWorkItem("cr.1", "/analysis"));
            AssertUtils.AssertThrows<ArgumentException>(
                () => _wp.CreateWorkItem(".cr1", "/analysis"));
            AssertUtils.AssertThrows<ArgumentException>(
                () => _wp.CreateWorkItem("cr1.", "/analysis"));
        }

        [TestMethod]
        public void ShouldCreateWorkItemWithSingleProperty()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");

            _wp.CreateWorkItem("cr1", "/analysis", new NameValueCollection { { "Name", "CR1" } });
            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(1, workItem.Properties.Count);
            Assert.AreEqual("CR1", workItem.Properties["Name"]);
        }

        [TestMethod]
        public void ShouldCreateWorkItemWithTwoProperties()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");

            _wp.CreateWorkItem("cr1", "/analysis", new NameValueCollection { { "Name", "CR1" }, { "Developers", "A, B" } });
            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(2, workItem.Properties.Count);
            Assert.AreEqual("CR1", workItem.Properties["Name"]);
            Assert.AreEqual("A, B", workItem.Properties["Developers"]);
        }

        [TestMethod]
        public void ShouldUpdateOneOfTwoProperties()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");

            _wp.CreateWorkItem("cr1", "/analysis", new NameValueCollection { { "Name", "CR1" }, { "Developer", "A" } });
            _wp.UpdateWorkItem("cr1", "/analysis", new NameValueCollection { { "Developer", "B" } });
            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(2, workItem.Properties.Count);
            Assert.AreEqual("CR1", workItem.Properties["Name"]);
            Assert.AreEqual("B", workItem.Properties["Developer"]);
        }

        [TestMethod]
        public void ShouldMoveUpdateOneOfTwoProperties()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr");

            _wp.CreateWorkItem("cr1", "/analysis", new NameValueCollection { { "Name", "CR1" }, { "Developer", "A" } });
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection { { "Developer", "B" } });
            var workItem = _wp.GetWorkItem("cr1");

            Assert.AreEqual(2, workItem.Properties.Count);
            Assert.AreEqual("/development", workItem.Path);
            Assert.AreEqual("CR1", workItem.Properties["Name"]);
            Assert.AreEqual("B", workItem.Properties["Developer"]);
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
