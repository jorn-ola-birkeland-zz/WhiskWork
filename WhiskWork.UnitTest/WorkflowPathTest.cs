using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Generic;
using System.Linq;
using WhiskWork.Test.Common;

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class WorkflowPathTest
    {
        [TestMethod]
        public void ShouldGiveRootWhenCombiningRoot()
        {
            Assert.AreEqual("/",WorkflowPath.CombinePath("/","/"));
        }

        [TestMethod]
        public void ShouldGivePathWhenCombiningPathWithRoot()
        {
            Assert.AreEqual("/path", WorkflowPath.CombinePath("/path", "/"));
        }

        [TestMethod]
        public void ShouldGivePathWhenCombiningRootWithPath()
        {
            Assert.AreEqual("/path", WorkflowPath.CombinePath("/", "/path"));
        }

        [TestMethod]
        public void ShouldGetAllSubPaths()
        {
            var result = WorkflowPath.GetSubPaths("/path/path1/path2/path3");
            Assert.IsTrue(result.SetEquals("/", "/path", "/path/path1", "/path/path1/path2", "/path/path1/path2/path3"));
        }

        [TestMethod]
        public void ShouldReturnSlashAsCommonRootForCompletelyDifferentPaths()
        {
            var result = WorkflowPath.FindCommonRoot("/path1", "/path2");
            Assert.AreEqual("/", result);
        }

        [TestMethod]
        public void ShouldReturnSelfAsCommonRootOfSelf()
        {
            const string path = "/path1/path2/path3";

            var result = WorkflowPath.FindCommonRoot(path, path);
            Assert.AreEqual(path, result);
        }

        [TestMethod]
        public void ShouldReturnCommonSubPathAsCommonRoot()
        {
            const string path1 = "/path1/path2/path3/path4";
            const string path2 = "/path1/path2/pathA/pathB";

            var result = WorkflowPath.FindCommonRoot(path1, path2);
            Assert.AreEqual("/path1/path2", result);
        }

        [TestMethod]
        public void ShouldReturnSubstepsBetweenTwoPaths()
        {
            const string path1 = "/path1/path2";
            const string path2 = "/path1/path2/pathA/pathB";

            var result = WorkflowPath.GetPathsBetween(path1, path2);

            Assert.AreEqual("/path1/path2,/path1/path2/pathA,/path1/path2/pathA/pathB", result.Join(','));
        }

        [TestMethod]
        public void ShouldLocateParentPath()
        {
            const string path = "/path1/path2";

            Assert.AreEqual("/path1",WorkflowPath.GetParentPath(path));
        }

        [TestMethod]
        public void ShouldReturnRootPathAsParent()
        {
            const string path = "/path1";

            Assert.AreEqual("/", WorkflowPath.GetParentPath(path));
        }

        [TestMethod]
        public void ShouldThrowExceptionIfGettingParentOfInvalidPath()
        {
            AssertUtils.AssertThrows<ArgumentException>(
                ()=>WorkflowPath.GetParentPath("invalidpath")
                );
        }

        [TestMethod]
        public void ShouldReturnLastPartOfPathAsLeafDirectory()
        {
            Assert.AreEqual("path1", WorkflowPath.GetLeafDirectory("/path1"));
            Assert.AreEqual("path3", WorkflowPath.GetLeafDirectory("/path1/path2/path3"));
        }

        [TestMethod]
        public void ShouldReturnNullAsLeafDirectoryForRootDirectory()
        {
            Assert.AreEqual(null, WorkflowPath.GetLeafDirectory("/"));
        }

    }
}
