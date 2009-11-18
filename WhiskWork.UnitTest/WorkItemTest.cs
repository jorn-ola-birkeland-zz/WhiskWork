using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Specialized;
using WhiskWork.Test.Common;

namespace WhiskWork.Core.UnitTest
{
    [TestClass]
    public class WorkItemTest
    {
        [TestMethod]
        public void ShouldNotCreateWorkItemWithSlashInId()
        {
            AssertUtils.AssertThrows<ArgumentException>(
                () => WorkItem.New("cr/1", "/analysis"));
            AssertUtils.AssertThrows<ArgumentException>(
                () => WorkItem.New("/cr1", "/analysis"));
            AssertUtils.AssertThrows<ArgumentException>(
                () => WorkItem.New("cr1/", "/analysis"));
        }

        [TestMethod]
        public void ShouldNotCreateWorkItemWithDotInId()
        {
            AssertUtils.AssertThrows<ArgumentException>(
                () => WorkItem.New("cr.1", "/analysis"));
            AssertUtils.AssertThrows<ArgumentException>(
                () => WorkItem.New(".cr1", "/analysis"));
            AssertUtils.AssertThrows<ArgumentException>(
                () => WorkItem.New("cr1.", "/analysis"));
        }

        [TestMethod]
        public void TwoEquallyCreatedWorkItemInstancesShouldEqual()
        {
            var wi1 = WorkItem.New("id", "path").UpdateOrdinal(1).AddClass("class").UpdateProperties(new NameValueCollection {{"prop1", "val1"}});
            var wi2 = WorkItem.New("id", "path").UpdateOrdinal(1).AddClass("class").UpdateProperties(new NameValueCollection { { "prop1", "val1" } });

            Assert.AreEqual(wi1,wi2);
        }

        [TestMethod]
        public void TwoEquallyCreatedWorkItemInstancesShouldHaveSameHashCode()
        {
            var wi1 = WorkItem.New("id", "path").UpdateOrdinal(1).AddClass("class").UpdateProperties(new NameValueCollection { { "prop1", "val1" } });
            var wi2 = WorkItem.New("id", "path").UpdateOrdinal(1).AddClass("class").UpdateProperties(new NameValueCollection { { "prop1", "val1" } });

            Assert.AreEqual(wi1.GetHashCode(), wi2.GetHashCode());
        }

        [TestMethod]
        public void TwoEquallyCreatedWorkItemInstancesShouldHaveSameToString()
        {
            var wi1 = WorkItem.New("id", "path").UpdateOrdinal(1).AddClass("class").UpdateProperties(new NameValueCollection { { "prop1", "val1" } });
            var wi2 = WorkItem.New("id", "path").UpdateOrdinal(1).AddClass("class").UpdateProperties(new NameValueCollection { { "prop1", "val1" } });

            Assert.AreEqual(wi1.ToString(), wi2.ToString());
        }

        [TestMethod]
        public void ShouldCreateChildItemWithCorrectParentId()
        {
            var parent = WorkItem.New("parentId", "path");
            var child = parent.CreateChildItem("childId");

            Assert.AreEqual(child.ParentId, parent.Id);
        }

        [TestMethod]
        public void ShouldCreateChildItemWithSameProperties()
        {
            var parent = WorkItem.New("parentId", "path");
            var child = parent.CreateChildItem("childId");

            Assert.AreEqual(parent.Ordinal,child.Ordinal);
            Assert.AreEqual(parent.Status, child.Status);
        }



    }
}
