using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Specialized;
using WhiskWork.Core;
using WhiskWork.Generic;

namespace WhiskWork.UnitTest
{
    [TestClass]
    public class ParallelWorkStepsTest
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
        public void ShouldGetAllSubsteps()
        {
            _workflowRepository.Add("/feedback", "/", 1, WorkStepType.Parallel, "cr");
            _workflowRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr cr-review");
            _workflowRepository.Add("/feedback/test", "/feedback", 2, WorkStepType.Normal, "cr cr-test");
            Assert.AreEqual(2, _workflowRepository.GetChildWorkSteps("/feedback").Count());
        }

        [TestMethod]
        public void ShouldFindSingleWorkItemAddedToAStep()
        {
            _workflowRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");
            _wp.CreateWorkItem("cr1", "/development");

            var item = _workItemRepository.GetWorkItem("cr1");
            
            Assert.AreEqual("cr1",item.Id);
            Assert.IsNull(null,item.ParentId);
            Assert.AreEqual("/development", item.Path);
            Assert.AreEqual(WorkItemStatus.Normal ,item.Status);
        }

        [TestMethod]
        public void ShouldNotCreateWorkItemInParallelStep()
        {
            CreateSimpleParallelWorkflow();
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.CreateWorkItem("cr1", "/feedback"));
        }

        [TestMethod]
        public void ShoudSplitWorkItem()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem("cr1", "/development");
            var item = _workItemRepository.GetWorkItem("cr1");

            var parallelStepHelper = new ParallelStepHelper(_workflowRepository);

            WorkStep feedbackStep = _workflowRepository.GetWorkStep("/feedback");

            IEnumerable<WorkItem> newWorkItems = parallelStepHelper.SplitForParallelism(item, feedbackStep);

            WorkItem reviewWorkItem = newWorkItems.ElementAt(0);
            WorkItem testWorkItem = newWorkItems.ElementAt(1);

            Assert.IsNotNull(reviewWorkItem);
            Assert.AreEqual("/development",reviewWorkItem.Path);
            Assert.AreEqual("cr cr-review",reviewWorkItem.Classes.Join(' '));
            Assert.IsNotNull(testWorkItem);
            Assert.AreEqual("/development", testWorkItem.Path);
            Assert.AreEqual("cr cr-test", testWorkItem.Classes.Join(' '));
        }

        [TestMethod]
        public void ShouldLockWorkItemAndCreateChildWorkItemsWhenMovedToParallelStep()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem("cr1", "/development");
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Select(wi => wi.Id == "cr1").Count());

            _wp.UpdateWorkItem("cr1", "/feedback/review", new NameValueCollection());

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1.review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1.test").Count());
            Assert.AreEqual(0, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1").Count());
        }

        [TestMethod]
        public void ShouldNotListParallelLockedWorkItem()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem("cr1", "/development");
            _wp.UpdateWorkItem("cr1", "/feedback/review", new NameValueCollection());

            Assert.AreEqual(0, _wp.GetWorkItems("/feedback").Where(wi => wi.Id == "cr1").Count());
        }

        [TestMethod]
        public void ShouldNotBeAbleToMoveParallelLockedWorkItem()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem("cr1", "/development");
            _wp.UpdateWorkItem("cr1", "/feedback/review", new NameValueCollection());

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.UpdateWorkItem("cr1", "/done", new NameValueCollection())
                );
        }


        [TestMethod]
        public void ShouldLockWorkItemAndCreateChildWorkItemsWhenMovedToRootOfParallelStep()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem("cr1", "/development");
            _wp.UpdateWorkItem("cr1", "/feedback", new NameValueCollection());

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1.review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1.test").Count());
        }

        [TestMethod]
        public void ShouldMoveSecondChildItemWhenParentParallelized()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem("cr1", "/development");
            _wp.UpdateWorkItem("cr1", "/feedback", new NameValueCollection());

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1.review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1.test").Count());

            _wp.UpdateWorkItem("cr1.test", "/feedback/test", new NameValueCollection());
            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/test").Where(wi => wi.Id == "cr1.test").Count());
        }

        [TestMethod]
        public void ShouldOnlyBeAbleToMoveChildItemToDedicatedParallelStep()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem("cr1", "/development");
            _wp.UpdateWorkItem("cr1", "/feedback", new NameValueCollection());

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1.review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1.test").Count());

            try
            {
                _wp.UpdateWorkItem("cr1.test", "/feedback/review", new NameValueCollection());
                Assert.Fail("Expected exception");
            }
            catch (InvalidOperationException)
            {
                Assert.IsTrue(true);

            }
        }

        [TestMethod]
        public void ShouldMergeChildItemsWhenMovedToSameStepOutsideParallelization()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem("cr1", "/development");
            _wp.UpdateWorkItem("cr1", "/feedback", new NameValueCollection());

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1.review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1.test").Count());

            _wp.UpdateWorkItem("cr1.test", "/done", new NameValueCollection());
            _wp.UpdateWorkItem("cr1.review", "/done", new NameValueCollection());
            Assert.AreEqual(1, _wp.GetWorkItems("/done").Where(wi => wi.Id == "cr1").Count());
            Assert.AreEqual(0, _wp.GetWorkItems("/done").Where(wi => wi.Id == "cr1.review").Count());
            Assert.AreEqual(0, _wp.GetWorkItems("/done").Where(wi => wi.Id == "cr1.test").Count());
        }



        private void CreateSimpleParallelWorkflow()
        {
            _workflowRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/feedback", "/", 2, WorkStepType.Parallel, "cr");
            _workflowRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr-review");
            _workflowRepository.Add("/feedback/test", "/feedback", 2, WorkStepType.Normal, "cr-test");
            _workflowRepository.Add("/done", "/", 2, WorkStepType.End, "cr");
        }

   }
}
