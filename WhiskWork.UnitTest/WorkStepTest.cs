using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Test.Common;

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class WorkStepTest
    {
        [TestMethod]
        public void ShouldOnlyCreateWorkStepsWithPathStartingWithSlash()
        {
            AssertUtils.AssertThrows<ArgumentException>(
                () => WorkStep.New("step")
                );
        }

        [TestMethod]
        public void ShouldCreateWorkStepsWithPathContainingSlash()
        {
             WorkStep.New("/step/substep");
        }

        [TestMethod]
        public void ShouldNotCreateWorkStepsWithPathContainingNeighbouringSlashes()
        {
            AssertUtils.AssertThrows<ArgumentException>(
                () => WorkStep.New("/step//substep")
                );
            AssertUtils.AssertThrows<ArgumentException>(
                () => WorkStep.New("//step")
                );
        }

        [TestMethod]
        public void ShouldCreateWorkStepsWithPathsWithCapitalAndNonCapitalAtoZAndNumbersAndHyphen()
        {
             WorkStep.New("/abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-");
        }

        [TestMethod]
        public void ShouldNotCreateWorkStepsWithPathWithTrailingSlash()
        {
            AssertUtils.AssertThrows<ArgumentException>(
                () => WorkStep.New("/step/")
                );
        }


        [TestMethod]
        public void TwoFullyAndEquallyDefinedWorkStepInstancesShouldEqual()
        {
            var ws1 = WorkStep.New("/path").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("class").UpdateTitle("title").UpdateWipLimit(2);
            var ws2 = WorkStep.New("/path").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("class").UpdateTitle("title").UpdateWipLimit(2);

            AssertAreEqual( ws1, ws2);
        }

        [TestMethod]
        public void ShouldEqualHaveSameHashCodeAndToStringIfPathMatch()
        {
            var ws1 = WorkStep.New("/path");
            var ws2 = WorkStep.New("/path");

            AssertAreEqual(ws1, ws2);
        }

        [TestMethod]
        public void ShouldNotEqualNotHaveSameHashCodeOrToStringIfPathVary()
        {
            var ws1 = WorkStep.New("/path1");
            var ws2 = WorkStep.New("/path2");
            
            AssertAreNotEqual(ws1,ws2);
        }

        [TestMethod,Ignore]
        public void ShouldEqualHaveSameHashCodeAndToStringIfPathAndOrdinalsMatch()
        {
            AssertEquality(ws=>ws.UpdateOrdinal(0),ws=>ws.UpdateOrdinal(1));
        }

        [TestMethod]
        public void ShouldEqualHaveSameHashCodeAndToStringIfPathAndTypesMatch()
        {
            AssertEquality(ws => ws.UpdateType(WorkStepType.Normal), ws => ws.UpdateType(WorkStepType.Expand));
        }

        [TestMethod]
        public void ShouldEqualHaveSameHashCodeAndToStringIfPathAndWorkItemClassMatch()
        {
            AssertEquality(ws => ws.UpdateWorkItemClass("class1"), ws => ws.UpdateWorkItemClass("class2"));
        }

        [TestMethod]
        public void ShouldEqualHaveSameHashCodeAndToStringIfPathAndTitleMatch()
        {
            AssertEquality(ws => ws.UpdateTitle("title1"), ws => ws.UpdateWorkItemClass("title2"));
        }

        [TestMethod]
        public void ShouldEqualHaveSameHashCodeAndToStringIfPathAndWipLimitMatch()
        {
            AssertEquality(ws => ws.UpdateWipLimit(1), ws => ws.UpdateWipLimit(2));
        }

        [TestMethod]
        public void ShouldNotAllowNullWorkItemClass()
        {
            AssertUtils.AssertThrows<ArgumentNullException>(
            () => WorkStep.New("/step").UpdateWorkItemClass(null)
                );
        }

        [TestMethod]
        public void ShouldNotAllowEmptyWorkItemClass()
        {
            AssertUtils.AssertThrows<ArgumentException>(
            () => WorkStep.New("/step").UpdateWorkItemClass(string.Empty)
                );
        }
        
        [TestMethod]
        public void ShouldNotAllowWorkItemClassWithSpace()
        {
            AssertUtils.AssertThrows<ArgumentException>(
            (   ) => WorkStep.New("/step").UpdateWorkItemClass("class 1")
                );
        }

        [TestMethod]
        public void ShouldAllowWorkItemClassWithHyphen()
        {
            WorkStep.New("/step").UpdateWorkItemClass("class-1");
        }

        [TestMethod]
        public void ShouldDefaultToNormalWorkStepType()
        {
            Assert.AreEqual(WorkStepType.Normal,WorkStep.New("/analysis").Type);
        }

        [TestMethod]
        public void ShouldDefaultToOrdinalNull()
        {
            Assert.AreEqual(null, WorkStep.New("/analysis").Ordinal);
        }

        [TestMethod]
        public void ShouldUpdateWorkItemClass()
        {
            var workStep = WorkStep.New("/analysis");
            Assert.AreEqual(null, workStep.WorkItemClass);
            Assert.AreEqual("cr", workStep.UpdateWorkItemClass("cr").WorkItemClass);
        }

        [TestMethod]
        public void ShouldUpdateOrdinal()
        {
            var workStep = WorkStep.New("/analysis");
            Assert.AreEqual(null, workStep.Ordinal);
            Assert.AreEqual(1, workStep.UpdateOrdinal(1).Ordinal);
        }

        [TestMethod]
        public void ShouldUpdateType()
        {
            var workStep = WorkStep.New("/analysis");
            Assert.AreEqual(WorkStepType.Normal, workStep.Type);
            Assert.AreEqual(WorkStepType.Begin, workStep.UpdateType(WorkStepType.Begin).Type);
        }

        [TestMethod]
        public void ShouldUpdateTitle()
        {
            var workStep = WorkStep.New("/analysis");
            Assert.AreEqual(null, workStep.Title);
            Assert.AreEqual("title", workStep.UpdateTitle("title").Title);
        }

        [TestMethod]
        public void ShouldUpdatePathAndParentPath()
        {
            var workStep = WorkStep.New("/analysis");

            var actualStep = workStep.UpdatePath("/dev/test");
            Assert.AreEqual("/dev/test", actualStep.Path);
            Assert.AreEqual("/dev", actualStep.ParentPath);
        }

        [TestMethod]
        public void ShouldNotUpdatePathAndParentPathWhenUpdatingFromOtherWorkStep()
        {
            var expected = WorkStep.New("/path1/path1sub");
            var update = WorkStep.New("/path2/path2sub");

            var actual = expected.UpdateFrom(update);

            Assert.AreEqual(expected.Path, actual.Path);
            Assert.AreEqual(expected.ParentPath, actual.ParentPath);
        }

        private void AssertEquality(Converter<WorkStep, WorkStep> setValue1, Converter<WorkStep, WorkStep> setValue2)
        {
            var workStep1WithValue1 = setValue1(WorkStep.New("/path"));
            var workStep2WithValue1 = setValue1(WorkStep.New("/path"));
            var workStepWithValue2 = setValue2(WorkStep.New("/path"));
            var workStepWithNoValueSet = setValue2(WorkStep.New("/path"));

            AssertAreEqual(workStep1WithValue1, workStep2WithValue1);
            AssertAreNotEqual(workStep1WithValue1, workStepWithValue2);
            AssertAreNotEqual(workStep1WithValue1, workStepWithNoValueSet);
        }
        
        private static void AssertAreEqual(WorkStep ws1, WorkStep ws2)
        {
            Assert.IsTrue(ws1.Equals(ws2));
            Assert.AreEqual(ws1.GetHashCode(),ws2.GetHashCode());
            Assert.AreEqual(ws1.ToString(),ws2.ToString());
        }

        private static void AssertAreNotEqual(WorkStep ws1, WorkStep ws2)
        {
            Assert.IsFalse(ws1.Equals(ws2));
            Assert.AreNotEqual(ws1.GetHashCode(), ws2.GetHashCode());
            Assert.AreNotEqual(ws1.ToString(), ws2.ToString());
        }

    }
}