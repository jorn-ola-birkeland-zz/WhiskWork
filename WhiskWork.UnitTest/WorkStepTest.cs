using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class WorkStepTest
    {
        [TestMethod]
        public void ShouldOnlyCreateWorkStepsWithPathStartingWithSlash()
        {
            AssertUtils.AssertThrows<ArgumentException>(
                () => new WorkStep("step", "/", 1, WorkStepType.Normal, "class")
                );
        }

        [TestMethod]
        public void ShouldCreateWorkStepsWithPathContainingSlash()
        {
                new WorkStep("/step/substep", "/", 1, WorkStepType.Normal, "class");
        }

        [TestMethod]
        public void ShouldNotCreateWorkStepsWithPathContainingNeighbouringSlashes()
        {
            AssertUtils.AssertThrows<ArgumentException>(
                () => new WorkStep("/step//substep", "/", 1, WorkStepType.Normal, "class")
                );
            AssertUtils.AssertThrows<ArgumentException>(
                () => new WorkStep("//step", "/", 1, WorkStepType.Normal, "class")
                );
        }
        
        [TestMethod]
        public void ShouldNotAllowParentPathNotBeingASubPathOfPath()
        {
            AssertUtils.AssertThrows<ArgumentException>(
                () => new WorkStep("/step/substep", "/step2", 1, WorkStepType.Normal, "class")
                );
            
        }

        [TestMethod]
        public void ShouldNotAllowDiffBetweenPathAndParentPathShouldToContainSlash2()
        {
            new WorkStep("/step", "/", 1, WorkStepType.Normal, "class");
        }


        [TestMethod]
        public void ShouldNotAllowDiffBetweenPathAndParentPathShouldToContainSlash()
        {
            AssertUtils.AssertThrows<ArgumentException>(
                () => new WorkStep("/step/substep/subsubstep", "/step", 1, WorkStepType.Normal, "class")
                );
        }


        [TestMethod]
        public void ShouldCreateWorkStepsWithPathsWithCapitalAndNonCapitalAtoZAndNumbersAndHyphen()
        {
             new WorkStep("/abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-", "/", 1,
                             WorkStepType.Normal,
                             "class");
        }

        [TestMethod]
        public void ShouldNotCreateWorkStepsWithPathWithTrailingSlash()
        {
            AssertUtils.AssertThrows<ArgumentException>(
                () => new WorkStep("/step/", "/", 1, WorkStepType.Normal, "class")
                );
        }


        [TestMethod]
        public void TwoEquallyCreatedWorkStepInstancesShouldEqual()
        {
            var ws1 = new WorkStep("/path", "/", 1, WorkStepType.Normal, "class");
            var ws2 = new WorkStep("/path", "/", 1, WorkStepType.Normal, "class");

            Assert.AreEqual(ws1, ws2);
        }

        [TestMethod]
        public void TwoEquallyCreatedWorkItemInstancesShouldHaveSameHashCode()
        {
            var ws1 = new WorkStep("/path", "/", 1, WorkStepType.Normal, "class");
            var ws2 = new WorkStep("/path", "/", 1, WorkStepType.Normal, "class");

            Assert.AreEqual(ws1.GetHashCode(), ws2.GetHashCode());
        }

        [TestMethod]
        public void TwoEquallyCreatedWorkItemInstancesShouldHaveSameToString()
        {
            var ws1 = new WorkStep("/path", "/", 1, WorkStepType.Normal, "class");
            var ws2 = new WorkStep("/path", "/", 1, WorkStepType.Normal, "class");

            Assert.AreEqual(ws1.ToString(), ws2.ToString());
        }
    }
}