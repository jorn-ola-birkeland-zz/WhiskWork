using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Generic;
using WhiskWork.Test.Common;

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class ParallelWorkStepsTest
    {
        private MemoryWorkStepRepository _workStepRepository;
        private MemoryWorkItemRepository _workItemRepository;
        private Workflow _wp;
        private WorkflowRepository _workflowRepository;

        [TestInitialize]
        public void Init()
        {
            _workStepRepository = new MemoryWorkStepRepository();
            _workItemRepository = new MemoryWorkItemRepository();
            _workflowRepository = new WorkflowRepository(_workItemRepository, _workStepRepository);
            _wp = new Workflow(_workflowRepository);
        }

        [TestMethod]
        public void ShouldGetAllSubsteps()
        {
            _workStepRepository.Add(WorkStep.New("/feedback").UpdateOrdinal(1).UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback/review").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback/test").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));
            Assert.AreEqual(2, _workStepRepository.GetChildWorkSteps("/feedback").Count());
        }

        [TestMethod]
        public void ShouldFindSingleWorkItemAddedToAStep()
        {
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));

            WorkItem item = _workItemRepository.GetWorkItem("cr1");

            Assert.AreEqual("cr1", item.Id);
            Assert.IsNull(item.Parent);
            Assert.AreEqual("/development", item.Path);
            Assert.AreEqual(WorkItemStatus.Normal, item.Status);
        }

        [TestMethod]
        public void ShouldNotCreateWorkItemInParallelStep()
        {
            CreateSimpleParallelWorkflow();
            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.CreateWorkItem(WorkItem.New("cr1","/feedback")));
        }

        [TestMethod]
        public void ShouldSplitWorkItem()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            var item = _workItemRepository.GetWorkItem("cr1");

            var parallelStepHelper = new ParallelStepHelper(_workflowRepository);

            var feedbackStep = _workStepRepository.GetWorkStep("/feedback");

            var newWorkItems = parallelStepHelper.SplitForParallelism(item, feedbackStep);

            var reviewWorkItem = newWorkItems.ElementAt(0);
            var testWorkItem = newWorkItems.ElementAt(1);

            Assert.IsNotNull(reviewWorkItem);
            Assert.AreEqual("/development", reviewWorkItem.Path);
            Assert.AreEqual("cr cr-review", reviewWorkItem.Classes.Join(' '));
            Assert.IsNotNull(testWorkItem);
            Assert.AreEqual("/development", testWorkItem.Path);
            Assert.AreEqual("cr cr-test", testWorkItem.Classes.Join(' '));
        }

        [TestMethod]
        public void ShouldLockWorkItemAndCreateChildWorkItemsWhenMovedToParallelStep()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Select(wi => wi.Id == "cr1").Count());

            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback/review"));

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1-test").Count());
            Assert.AreEqual(0, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1").Count());
        }

        [TestMethod]
        public void ShouldAlsoParalleledChildrenWhenDeletingParalleledWorkItem()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback/review"));

            Assert.IsTrue(_wp.ExistsWorkItem("cr1"));
            Assert.IsTrue(_wp.ExistsWorkItem("cr1-review"));
            Assert.IsTrue(_wp.ExistsWorkItem("cr1-test"));

            _wp.DeleteWorkItem("cr1");

            Assert.IsFalse(_wp.ExistsWorkItem("cr1"));
            Assert.IsFalse(_wp.ExistsWorkItem("cr1-review"));
            Assert.IsFalse(_wp.ExistsWorkItem("cr1-test"));
        }

        [TestMethod]
        public void ShouldNotBeAbleToMoveParallelLockedWorkItem()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback/review"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.MoveWorkItem("/done","cr1"));
        }

        [TestMethod]
        public void ShouldUpdateParallelLockedWorkItem()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem(WorkItem.New("cr1", "/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback/review"));

            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback").UpdateProperty("Test", "test"));

            Assert.AreEqual("test", _wp.GetWorkItems("/feedback").Where(wi => wi.Id == "cr1").Single().Properties["Test"]);
            
        }


        [TestMethod]
        public void ShouldLockWorkItemAndCreateChildWorkItemsWhenMovedToRootOfParallelStep()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback"));

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1-test").Count());
        }

        [TestMethod]
        public void ShouldMoveSecondChildItemWhenParentParallelized()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback"));

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1-test").Count());

            _wp.UpdateWorkItem(WorkItem.New("cr1-test", "/feedback/test"));
            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/test").Where(wi => wi.Id == "cr1-test").Count());
        }

        [TestMethod]
        public void ShouldOnlyBeAbleToMoveChildItemToDedicatedParallelStep()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback"));

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1-test").Count());


            AssertUtils.AssertThrows<InvalidOperationException>(
                ()=> _wp.UpdateWorkItem(WorkItem.New("cr1-test", "/feedback/review")));

            Assert.AreEqual(1, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1-test").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1-review").Count());
        }

        [TestMethod]
        public void ShouldNotBeAbleToDeleteParallelledChildOfParalleledWorkItem()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback/review"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () =>
                _wp.DeleteWorkItem("cr1-test")
                );
        }

        [TestMethod]
        public void ShouldMergeChildItemsWhenBothMovedToSameNormalStepOutsideParallelization()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem("/development", "cr1");
            _wp.MoveWorkItem("/feedback", "cr1");

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1-test").Count());

            _wp.MoveWorkItem("/done", "cr1-test", "cr1-review");

            Assert.AreEqual(1, _wp.GetWorkItems("/done").Where(wi => wi.Id == "cr1").Count());
            Assert.AreEqual(0, _wp.GetWorkItems("/done").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(0, _wp.GetWorkItems("/done").Where(wi => wi.Id == "cr1-test").Count());
        }

        [TestMethod]
        public void ShouldMergeChildItemsWhenBothMovedToExpandStep()
        {
            CreateParallelWorkflowWithExpandStep();

            _wp.CreateWorkItem("/scheduled","cr1");
            _wp.MoveWorkItem("/feedback","cr1");

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/scheduled").Where(wi => wi.Id == "cr1-test").Count());

            _wp.MoveWorkItem("/development/inprocess", "cr1-test","cr1-review");

            Assert.AreEqual(0, _wp.GetWorkItems("/development/inprocess").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(0, _wp.GetWorkItems("/development/inprocess").Where(wi => wi.Id == "cr1-test").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development/inprocess").Where(wi => wi.Id == "cr1").Count());
        }

        [TestMethod]
        public void ShouldRemoveTransientStepsWhenChildItemsAreMergedInExpandStep()
        {
            CreateParallelWorkflowWithExpandStep();

            _wp.CreateWorkItem("/scheduled", "cr1");
            _wp.MoveWorkItem("/feedback", "cr1");

            _wp.MoveWorkItem("/development/inprocess", "cr1-test");

            Assert.IsTrue(_wp.ExistsWorkStep("/development/inprocess/cr1-test/tasks"));

            _wp.MoveWorkItem("/development/inprocess", "cr1-review");

            Assert.IsFalse(_wp.ExistsWorkStep("/development/inprocess/cr1-test/tasks"));
        }

        [TestMethod]
        public void ShouldCreateTransientStepsForParentWhenChildItemsHaveBeenMergedInExpandStep()
        {
            CreateParallelWorkflowWithExpandStep();

            _wp.CreateWorkItem("/scheduled", "cr1");
            _wp.MoveWorkItem("/feedback", "cr1");

            Assert.IsFalse(_wp.ExistsWorkStep("/development/inprocess/cr1/tasks"));
            _wp.MoveWorkItem("/development/inprocess", "cr1-test");

            Assert.IsFalse(_wp.ExistsWorkStep("/development/inprocess/cr1/tasks"));
            _wp.MoveWorkItem("/development/inprocess", "cr1-review");

            Assert.IsTrue(_wp.ExistsWorkStep("/development/inprocess/cr1/tasks"));
        }
        
        [TestMethod]
        public void ShouldMergeParallelledChildItemsWhenMovedToSameStepOutsideParallelizationAndExpandChildWorkItemWasCreatedInExpandStep ()
        {
            CreateParallelWorkflowWithExpandStep();

            _wp.CreateWorkItem(WorkItem.New("cr1","/scheduled"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));
            _wp.CreateWorkItem(WorkItem.New("cr1-1","/development/inprocess/cr1/tasks"));
            _wp.UpdateWorkItem(WorkItem.New("cr1-1", "/development/inprocess/cr1/tasks/done"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development/done"));

            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback"));
            _wp.UpdateWorkItem(WorkItem.New("cr1-test", "/feedback/test"));

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/test").Where(wi => wi.Id == "cr1-test").Count());

            _wp.UpdateWorkItem(WorkItem.New("cr1-test", "/done"));
            _wp.UpdateWorkItem(WorkItem.New("cr1-review", "/done"));
            Assert.AreEqual(0, _wp.GetWorkItems("/done").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(0, _wp.GetWorkItems("/done").Where(wi => wi.Id == "cr1-test").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/done").Where(wi => wi.Id == "cr1").Count());
        }

        [TestMethod]
        public void ShouldMoveFromExpandStepToParallelStepAndCreateTransientStepsForParallelledSibling()
        {
            CreateParallelWorkflowWithExpandStep();

            _wp.CreateWorkItem("/scheduled", "cr1");
            _wp.MoveWorkItem("/development","cr1");
            Assert.IsTrue(_wp.ExistsWorkStep("/development/inprocess/cr1/tasks"));

            _wp.MoveWorkItem("/feedback/review","cr1");

            Assert.AreEqual("/feedback",_wp.GetWorkItem("cr1").Path);
            Assert.IsTrue(_wp.ExistsWorkStep("/development/inprocess/cr1-test/tasks"));
            Assert.AreEqual("/feedback/review", _wp.GetWorkItem("cr1-review").Path);
        }

        [TestMethod]
        public void ShouldBeAbleToMoveParalleledWorkItemToExpandStep()
        {
            CreateParallelWorkflowWithExpandStep();

            _wp.CreateWorkItem(WorkItem.New("cr1","/scheduled"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback/review"));

            _wp.UpdateWorkItem(WorkItem.New("cr1-review", "/development/inprocess"));

            var workItem = _wp.GetWorkItem("cr1-review");
            Assert.AreEqual("/development/inprocess", workItem.Path);
        }

        [TestMethod]
        public void ShouldRemoveTransientStepWhenChildrenOfParallelledWorkItemAreMerged()
        {
            CreateParallelWorkflowWithExpandStep();

            _wp.CreateWorkItem(WorkItem.New("cr1","/scheduled"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback/review"));

            _wp.UpdateWorkItem(WorkItem.New("cr1-review", "/development/inprocess"));
            Assert.IsTrue(_wp.ExistsWorkStep("/development/inprocess/cr1-review"));

            _wp.UpdateWorkItem(WorkItem.New("cr1-review", "/scheduled"));
            Assert.IsFalse(_wp.ExistsWorkStep("/development/inprocess/cr1-review"));
        }

        [TestMethod]
        public void ShouldNotBeAbleToMoveParallelledWorkItemToTransientStepOfSibling()
        {
            CreateParallelWorkflowWithExpandStep();

            _wp.CreateWorkItem(WorkItem.New("cr1","/scheduled"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback/review"));

            _wp.UpdateWorkItem(WorkItem.New("cr1-review", "/development/inprocess"));

            AssertUtils.AssertThrows<InvalidOperationException>(
            () => _wp.UpdateWorkItem(WorkItem.New("cr1-test", "/development/inprocess/cr1-review")));
        }

        [TestMethod]
        public void ShouldMoveToLeafStepUnderParallelStep()
        {
            _workStepRepository.Add(WorkStep.New("/planned").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/implementation").UpdateOrdinal(2).UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/implementation/dev").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-dev"));
            _workStepRepository.Add(WorkStep.New("/implementation/doc").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-doc"));
            _workStepRepository.Add(WorkStep.New("/implementation/dev/coding").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-dev"));

            _wp.CreateWorkItem("/planned", "cr1");
            _wp.MoveWorkItem("/implementation", "cr1");

            Assert.AreEqual("/implementation/dev/coding",_wp.GetWorkItem("cr1-dev").Path);
        }

        [TestMethod]
        public void ShouldHandleNestedParallelSteps()
        {
            _workStepRepository.Add(WorkStep.New("/planned").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/implementation").UpdateOrdinal(2).UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/implementation/dev").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-dev"));
            _workStepRepository.Add(WorkStep.New("/implementation/doc").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-doc"));
            _workStepRepository.Add(WorkStep.New("/implementation/dev/coding").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-dev"));
            _workStepRepository.Add(WorkStep.New("/implementation/dev/feedback").UpdateOrdinal(2).UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr-dev"));
            _workStepRepository.Add(WorkStep.New("/implementation/dev/feedback/review").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-dev-review"));
            _workStepRepository.Add(WorkStep.New("/implementation/dev/feedback/test").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-dev-test"));
            _workStepRepository.Add(WorkStep.New("/done").UpdateOrdinal(3).UpdateType(WorkStepType.End).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem("/planned","cr1");
            _wp.MoveWorkItem("/implementation","cr1");

            Assert.AreEqual("/planned",_wp.GetWorkItem("cr1-doc").Path);
            Assert.AreEqual("/implementation/dev/coding", _wp.GetWorkItem("cr1-dev").Path);

            _wp.MoveWorkItem("/implementation/dev/feedback", "cr1-dev");
            Assert.AreEqual("/implementation/dev/coding", _wp.GetWorkItem("cr1-dev-test").Path);
            Assert.AreEqual("/implementation/dev/feedback/review", _wp.GetWorkItem("cr1-dev-review").Path);
        }

        [TestMethod]
        public void ShouldMoveChildOfParallelledWorkItemToSubsequentParallelStep()
        {
            _workStepRepository.Add(WorkStep.New("/planned").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/implementation").UpdateOrdinal(2).UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/implementation/dev").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-dev"));
            _workStepRepository.Add(WorkStep.New("/implementation/doc").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-doc"));
            _workStepRepository.Add(WorkStep.New("/feedback").UpdateOrdinal(3).UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback/review").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-review"));
            _workStepRepository.Add(WorkStep.New("/feedback/test").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-test"));

            _wp.CreateWorkItem("/planned", "cr1");
            _wp.MoveWorkItem("/implementation", "cr1");

            Assert.AreEqual("/planned", _wp.GetWorkItem("cr1-doc").Path);
            Assert.AreEqual("/implementation/dev", _wp.GetWorkItem("cr1-dev").Path);

            _wp.MoveWorkItem("/feedback", "cr1-dev");

            Assert.AreEqual("/implementation/dev", _wp.GetWorkItem("cr1-dev-test").Path);
            Assert.AreEqual("/feedback/review", _wp.GetWorkItem("cr1-dev-review").Path);
        }


        [TestMethod]
        public void ShouldMoveChildOfParallelledWorkItemToSubsequentParallelStepAndMergeUponReturn()
        {
            ShouldMoveChildOfParallelledWorkItemToSubsequentParallelStep();
            _wp.MoveWorkItem("/implementation/dev", "cr1-dev-review");

            Assert.IsFalse(_wp.ExistsWorkItem("cr1-dev-review"));
            Assert.IsFalse(_wp.ExistsWorkItem("cr1-dev-test"));

            Assert.AreEqual("/implementation/dev", _wp.GetWorkItem("cr1-dev").Path);
        }

        [TestMethod]
        public void ShouldKeepAddingChildItemsWhenMovingBetweenParallelSteps()
        {
            ShouldMoveChildOfParallelledWorkItemToSubsequentParallelStep();

            _wp.MoveWorkItem("/implementation/doc", "cr1-dev-review");
            Assert.AreEqual("/implementation/doc", _wp.GetWorkItem("cr1-dev-review-doc").Path);
        }


        
        private void CreateSimpleParallelWorkflow()
        {
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback").UpdateOrdinal(2).UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback/review").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-review"));
            _workStepRepository.Add(WorkStep.New("/feedback/test").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-test"));
            _workStepRepository.Add(WorkStep.New("/done").UpdateOrdinal(2).UpdateType(WorkStepType.End).UpdateWorkItemClass("cr"));
        }

        private void CreateParallelWorkflowWithExpandStep()
        {
            _workStepRepository.Add(WorkStep.New("/scheduled").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr").UpdateTitle("Scheduled"));
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr").UpdateTitle("Analysis"));
            _workStepRepository.Add(WorkStep.New("/analysis/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/analysis/done").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr").UpdateTitle("Development"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess/tasks").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("task").UpdateTitle("Tasks"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess/tasks/new").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("task"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess/tasks/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("task"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess/tasks/done").UpdateOrdinal(1).UpdateType(WorkStepType.End).UpdateWorkItemClass("task"));
            _workStepRepository.Add(WorkStep.New("/development/done").UpdateOrdinal(2).UpdateType(WorkStepType.End).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback").UpdateOrdinal(3).UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback/review").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-review").UpdateTitle("Review"));
            _workStepRepository.Add(WorkStep.New("/feedback/test").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-test").UpdateTitle("Test"));
            _workStepRepository.Add(WorkStep.New("/done").UpdateOrdinal(4).UpdateType(WorkStepType.End).UpdateWorkItemClass("cr").UpdateTitle("Done"));
        }
    }
}   