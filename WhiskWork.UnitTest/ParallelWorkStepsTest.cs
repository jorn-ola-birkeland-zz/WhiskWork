using System;
using System.Collections.Generic;
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

        [TestInitialize]
        public void Init()
        {
            _workStepRepository = new MemoryWorkStepRepository();
            _workItemRepository = new MemoryWorkItemRepository();
            _wp = new Workflow(_workStepRepository, _workItemRepository);
        }

        [TestMethod]
        public void ShouldGetAllSubsteps()
        {
            _workStepRepository.Add("/feedback", "/", 1, WorkStepType.Parallel, "cr");
            _workStepRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr");
            _workStepRepository.Add("/feedback/test", "/feedback", 2, WorkStepType.Normal, "cr");
            Assert.AreEqual(2, _workStepRepository.GetChildWorkSteps("/feedback").Count());
        }

        [TestMethod]
        public void ShouldFindSingleWorkItemAddedToAStep()
        {
            _workStepRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");
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
            WorkItem item = _workItemRepository.GetWorkItem("cr1");

            var parallelStepHelper = new ParallelStepHelper(_workStepRepository);

            WorkStep feedbackStep = _workStepRepository.GetWorkStep("/feedback");

            IEnumerable<WorkItem> newWorkItems = parallelStepHelper.SplitForParallelism(item, feedbackStep);

            WorkItem reviewWorkItem = newWorkItems.ElementAt(0);
            WorkItem testWorkItem = newWorkItems.ElementAt(1);

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
        public void ShouldNotListParallelLockedWorkItem()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback/review"));

            Assert.AreEqual(0, _wp.GetWorkItems("/feedback").Where(wi => wi.Id == "cr1").Count());
        }

        [TestMethod]
        public void ShouldNotBeAbleToMoveParallelLockedWorkItem()
        {
            CreateSimpleParallelWorkflow();

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback/review"));

            AssertUtils.AssertThrows<InvalidOperationException>(
                () => _wp.UpdateWorkItem(WorkItem.New("cr1", "/done")));
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

            _wp.Create("/development", "cr1");
            _wp.Move("/feedback", "cr1");

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development").Where(wi => wi.Id == "cr1-test").Count());

            _wp.Move("/done", "cr1-test", "cr1-review");

            Assert.AreEqual(1, _wp.GetWorkItems("/done").Where(wi => wi.Id == "cr1").Count());
            Assert.AreEqual(0, _wp.GetWorkItems("/done").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(0, _wp.GetWorkItems("/done").Where(wi => wi.Id == "cr1-test").Count());
        }

        [TestMethod]
        public void ShouldMergeChildItemsWhenBothMovedToExpandStep()
        {
            CreateParallelWorkflowWithExpandStep();

            _wp.Create("/scheduled","cr1");
            _wp.Move("/feedback","cr1");

            Assert.AreEqual(1, _wp.GetWorkItems("/feedback/review").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/scheduled").Where(wi => wi.Id == "cr1-test").Count());

            _wp.Move("/development/inprocess", "cr1-test","cr1-review");

            Assert.AreEqual(0, _wp.GetWorkItems("/development/inprocess").Where(wi => wi.Id == "cr1-review").Count());
            Assert.AreEqual(0, _wp.GetWorkItems("/development/inprocess").Where(wi => wi.Id == "cr1-test").Count());
            Assert.AreEqual(1, _wp.GetWorkItems("/development/inprocess").Where(wi => wi.Id == "cr1").Count());
        }

        [TestMethod]
        public void ShouldRemoveTransientStepsWhenChildItemsAreMergedInExpandStep()
        {
            CreateParallelWorkflowWithExpandStep();

            _wp.Create("/scheduled", "cr1");
            _wp.Move("/feedback", "cr1");

            _wp.Move("/development/inprocess", "cr1-test");

            Assert.IsTrue(_wp.ExistsWorkStep("/development/inprocess/cr1-test/tasks"));

            _wp.Move("/development/inprocess", "cr1-review");

            Assert.IsFalse(_wp.ExistsWorkStep("/development/inprocess/cr1-test/tasks"));
        }

        [TestMethod]
        public void ShouldCreateTransientStepsForParentWhenChildItemsHaveBeenMergedInExpandStep()
        {
            CreateParallelWorkflowWithExpandStep();

            _wp.Create("/scheduled", "cr1");
            _wp.Move("/feedback", "cr1");

            Assert.IsFalse(_wp.ExistsWorkStep("/development/inprocess/cr1/tasks"));
            _wp.Move("/development/inprocess", "cr1-test");

            Assert.IsFalse(_wp.ExistsWorkStep("/development/inprocess/cr1/tasks"));
            _wp.Move("/development/inprocess", "cr1-review");

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

            _wp.Create("/scheduled", "cr1");
            _wp.Move("/development","cr1");
            Assert.IsTrue(_wp.ExistsWorkStep("/development/inprocess/cr1/tasks"));

            _wp.Move("/feedback/review","cr1");

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
            _workStepRepository.Add("/planned", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/implementation", "/", 2, WorkStepType.Parallel, "cr");
            _workStepRepository.Add("/implementation/dev", "/implementation", 1, WorkStepType.Normal, "cr-dev");
            _workStepRepository.Add("/implementation/doc", "/implementation", 2, WorkStepType.Normal, "cr-doc");
            _workStepRepository.Add("/implementation/dev/coding", "/implementation/dev", 1, WorkStepType.Normal, "cr-dev");

            _wp.Create("/planned", "cr1");
            _wp.Move("/implementation", "cr1");

            Assert.AreEqual("/implementation/dev/coding",_wp.GetWorkItem("cr1-dev").Path);
        }

        [TestMethod]
        public void ShouldHandleNestedParallelSteps()
        {
            _workStepRepository.Add("/planned", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/implementation", "/", 2, WorkStepType.Parallel, "cr");
            _workStepRepository.Add("/implementation/dev", "/implementation", 1, WorkStepType.Normal, "cr-dev");
            _workStepRepository.Add("/implementation/doc", "/implementation", 2, WorkStepType.Normal, "cr-doc");
            _workStepRepository.Add("/implementation/dev/coding", "/implementation/dev", 1, WorkStepType.Normal, "cr-dev");
            _workStepRepository.Add("/implementation/dev/feedback", "/implementation/dev", 2, WorkStepType.Parallel, "cr-dev");
            _workStepRepository.Add("/implementation/dev/feedback/review", "/implementation/dev/feedback", 1, WorkStepType.Normal, "cr-dev-review");
            _workStepRepository.Add("/implementation/dev/feedback/test", "/implementation/dev/feedback", 2, WorkStepType.Normal, "cr-dev-test");
            _workStepRepository.Add("/done", "/", 3, WorkStepType.End, "cr");

            _wp.Create("/planned","cr1");
            _wp.Move("/implementation","cr1");

            Assert.AreEqual("/planned",_wp.GetWorkItem("cr1-doc").Path);
            Assert.AreEqual("/implementation/dev/coding", _wp.GetWorkItem("cr1-dev").Path);

            _wp.Move("/implementation/dev/feedback", "cr1-dev");
            Assert.AreEqual("/implementation/dev/coding", _wp.GetWorkItem("cr1-dev-test").Path);
            Assert.AreEqual("/implementation/dev/feedback/review", _wp.GetWorkItem("cr1-dev-review").Path);
        }

        [TestMethod]
        public void ShouldMoveChildOfParallelledWorkItemToSubsequentParallelStep()
        {
            _workStepRepository.Add("/planned", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/implementation", "/", 2, WorkStepType.Parallel, "cr");
            _workStepRepository.Add("/implementation/dev", "/implementation", 1, WorkStepType.Normal, "cr-dev");
            _workStepRepository.Add("/implementation/doc", "/implementation", 2, WorkStepType.Normal, "cr-doc");
            _workStepRepository.Add("/feedback", "/", 3, WorkStepType.Parallel, "cr");
            _workStepRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr-review");
            _workStepRepository.Add("/feedback/test", "/feedback", 2, WorkStepType.Normal, "cr-test");

            _wp.Create("/planned", "cr1");
            _wp.Move("/implementation", "cr1");

            Assert.AreEqual("/planned", _wp.GetWorkItem("cr1-doc").Path);
            Assert.AreEqual("/implementation/dev", _wp.GetWorkItem("cr1-dev").Path);

            _wp.Move("/feedback", "cr1-dev");

            Assert.AreEqual("/implementation/dev", _wp.GetWorkItem("cr1-dev-test").Path);
            Assert.AreEqual("/feedback/review", _wp.GetWorkItem("cr1-dev-review").Path);
        }


        [TestMethod]
        public void ShouldMoveChildOfParallelledWorkItemToSubsequentParallelStepAndMergeUponReturn()
        {
            ShouldMoveChildOfParallelledWorkItemToSubsequentParallelStep();
            _wp.Move("/implementation/dev", "cr1-dev-review");

            Assert.IsFalse(_wp.ExistsWorkItem("cr1-dev-review"));
            Assert.IsFalse(_wp.ExistsWorkItem("cr1-dev-test"));

            Assert.AreEqual("/implementation/dev", _wp.GetWorkItem("cr1-dev").Path);
        }

        [TestMethod]
        public void ShouldKeepAddingChildItemsWhenMovingBetweenParallelSteps()
        {
            ShouldMoveChildOfParallelledWorkItemToSubsequentParallelStep();

            _wp.Move("/implementation/doc", "cr1-dev-review");
            Assert.AreEqual("/implementation/doc", _wp.GetWorkItem("cr1-dev-review-doc").Path);
        }
        
        private void CreateSimpleParallelWorkflow()
        {
            _workStepRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/feedback", "/", 2, WorkStepType.Parallel, "cr");
            _workStepRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr-review");
            _workStepRepository.Add("/feedback/test", "/feedback", 2, WorkStepType.Normal, "cr-test");
            _workStepRepository.Add("/done", "/", 2, WorkStepType.End, "cr");
        }

        private void CreateParallelWorkflowWithExpandStep()
        {
            _workStepRepository.Add("/scheduled", "/", 1, WorkStepType.Begin, "cr", "Scheduled");
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Normal, "cr", "Analysis");
            _workStepRepository.Add("/analysis/inprocess", "/analysis", 1, WorkStepType.Normal, "cr");
            _workStepRepository.Add("/analysis/done", "/analysis", 1, WorkStepType.Normal, "cr");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr", "Development");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workStepRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal,
                                    "task", "Tasks");
            _workStepRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1,
                                    WorkStepType.Begin, "task");
            _workStepRepository.Add("/development/inprocess/tasks/inprocess", "/development/inprocess/tasks", 1,
                                    WorkStepType.Normal, "task");
            _workStepRepository.Add("/development/inprocess/tasks/done", "/development/inprocess/tasks", 1,
                                    WorkStepType.End, "task");
            _workStepRepository.Add("/development/done", "/development", 2, WorkStepType.End, "cr");
            _workStepRepository.Add("/feedback", "/", 3, WorkStepType.Parallel, "cr");
            _workStepRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr-review", "Review");
            _workStepRepository.Add("/feedback/test", "/feedback", 2, WorkStepType.Normal, "cr-test", "Test");
            _workStepRepository.Add("/done", "/", 4, WorkStepType.End, "cr", "Done");
        }
    }
}   