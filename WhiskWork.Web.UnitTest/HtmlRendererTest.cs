using System;
using System.Collections.Specialized;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Core;
using System.IO;
using WhiskWork.Test.Common;
using WhiskWork.Web.UnitTest.Properties;

namespace WhiskWork.Web.UnitTest
{
    [TestClass]
    public class HtmlRendererTest
    {
        private MemoryWorkStepRepository _workStepRepository;
        private MemoryWorkItemRepository _workItemRepository;
        private Workflow _wp;

        [TestInitialize]
        public void Init()
        {
            _workStepRepository = new MemoryWorkStepRepository();
            _workItemRepository = new MemoryWorkItemRepository();
            _wp = new Workflow(new WorkflowRepository(_workItemRepository, _workStepRepository));

        }
            

        [TestMethod]
        public void ShouldRenderSingleStepWorkflow()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            var doc = GetDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]"));
        }

        [TestMethod]
        public void ShouldRenderSingleStepWithTitleWorkflow()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr").UpdateTitle("Analysis"));

            var doc = GetDocument();

            Assert.AreEqual("Analysis", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/h1").InnerText.Trim());
        }


        [TestMethod]
        public void ShouldRenderStepsInRightOrder()
        {
            _workStepRepository.Add(WorkStep.New("/test").UpdateOrdinal(3).UpdateType(WorkStepType.End).UpdateWorkItemClass("cr").UpdateTitle("Test"));
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr").UpdateTitle("Analysis"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr").UpdateTitle("Development"));

            var doc = GetDocument();

            Assert.AreEqual("analysis",doc.SelectSingleNode("/html/body/ol/li[position()=1]/@id").Value);
            Assert.AreEqual("development", doc.SelectSingleNode("/html/body/ol/li[position()=2]/@id").Value);
            Assert.AreEqual("test", doc.SelectSingleNode("/html/body/ol/li[position()=3]/@id").Value);


        }

        [TestMethod]
        public void ShouldRenderSubSteps()
        {
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr").UpdateTitle("Development "));
            _workStepRepository.Add(WorkStep.New("/development/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr").UpdateTitle("In process"));
            _workStepRepository.Add(WorkStep.New("/development/done").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr").UpdateTitle("Dev. done"));

            var doc = GetDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li[@id=\"development.inprocess\"]"));
            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li[@id=\"development.done\"]"));
        }

        [TestMethod]
        public void ShouldRenderClasses()
        {
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr").UpdateTitle("Development "));
            _workStepRepository.Add(WorkStep.New("/development/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr").UpdateTitle("In process"));
            _workStepRepository.Add(WorkStep.New("/development/done").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr").UpdateTitle("Dev. done"));

            var doc = GetDocument();

            Assert.AreEqual("workstep step-cr inprocess", doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderSingleWorkItemInSingleStepWorkflow()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr").UpdateTitle("Analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));

            var doc = GetDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li[@id=\"cr1\"]"));
        }

        [TestMethod]
        public void ShouldRenderSingleWorkItemProperty()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr").UpdateTitle("Analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis",new NameValueCollection {{"Name","CR1"}}));

            var doc = GetDocument();

            Assert.AreEqual("Name", doc.SelectSingleNode("//li[@id=\"cr1\"]/dl/dt[@class='name']").InnerText);
            Assert.AreEqual("CR1", doc.SelectSingleNode("//li[@id=\"cr1\"]/dl/dd[@class='name']").InnerText);
        }

        [TestMethod]
        public void ShouldRenderTwoWorkItemProperties()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr").UpdateTitle("Analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis",new NameValueCollection { { "Name", "CR1" },{"Developer","A"} }));

            var doc = GetDocument();

            Assert.AreEqual("Name", doc.SelectSingleNode("//li[@id=\"cr1\"]/dl/dt[@class='name']").InnerText);
            Assert.AreEqual("CR1", doc.SelectSingleNode("//li[@id=\"cr1\"]/dl/dd[@class='name']").InnerText);

            Assert.AreEqual("Developer", doc.SelectSingleNode("//li[@id=\"cr1\"]/dl/dt[@class='developer']").InnerText);
            Assert.AreEqual("A", doc.SelectSingleNode("//li[@id=\"cr1\"]/dl/dd[@class='developer']").InnerText);
        }

        [TestMethod]
        public void ShouldLowerCasePropertyKeyInClass()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr").UpdateTitle("Analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis",new NameValueCollection { { "Name", "value" } }));

            var doc = GetDocument();

            Assert.AreEqual("name", doc.SelectSingleNode("//li[@id=\"cr1\"]/dl/dt/@class").InnerText);
        }


        [TestMethod]
        public void ShouldHtmlEncodePropertyValues()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr").UpdateTitle("Analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis",new NameValueCollection { { "name", "&<> " } }));

            var doc = GetDocument();

            Assert.AreEqual("&<> ", doc.SelectSingleNode("//li[@id=\"cr1\"]/dl/dd[@class='name']").InnerText);
        }

        [TestMethod]
        public void ShouldHtmlEncodeTitle()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr").UpdateTitle("<Analysis & Test>"));

            var doc = GetDocument();

            Assert.AreEqual("<Analysis & Test>", doc.SelectSingleNode("//h1").InnerText.Trim());
        }


        [TestMethod]
        public void ShouldRenderTwoWorkItemsInSingleStepWorkflow()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr").UpdateTitle("Analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr2","/analysis"));

            var doc = GetDocument();

            Assert.AreEqual("cr1", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li[position()=1]/@id").Value);
            Assert.AreEqual("cr2", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li[position()=2]/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderAWorkItemInEachStepForAThreeStepWorkflow()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/done").UpdateOrdinal(3).UpdateType(WorkStepType.End).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr2","/analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr3","/analysis"));

            _wp.UpdateWorkItem(WorkItem.New("cr2", "/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr3", "/done"));

            var doc = GetDocument();

            Assert.AreEqual("cr1", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li/@id").Value);
            Assert.AreEqual("cr2", doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li/@id").Value);
            Assert.AreEqual("cr3", doc.SelectSingleNode("/html/body/ol/li[@id=\"done\"]/ol/li/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderParallelStepAsUnorderedList()
        {
            _workStepRepository.Add(WorkStep.New("/feedback").UpdateOrdinal(1).UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback/review").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback/test").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-test"));

            var doc = GetDocument();

            Assert.AreEqual("feedback.review", doc.SelectSingleNode("/html/body/ol/li[@id=\"feedback\"]/ul/li[position()=1]/@id").Value);
            Assert.AreEqual("feedback.test", doc.SelectSingleNode("/html/body/ol/li[@id=\"feedback\"]/ul/li[position()=2]/@id").Value);
        }


        [TestMethod]
        public void ShouldNotRenderEmptyListTagInLeafStepWithNoWorkItems()
        {
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            var doc = GetDocument();

            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"development\"]/ol"));
            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"development\"]/ul"));
        }

        [TestMethod]
        public void ShouldNotRenderH1TagIfWorkStepTitleIsEmpty()
        {
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            var doc = GetDocument();

            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"development\"]/h1"));
        }


        [TestMethod]
        public void ShouldRenderWorkItemWithRightClasses()
        {
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));

            var doc = GetDocument();

            var classAttribute = doc.SelectSingleNode("//li[@id=\"development\"]/ol/li[@id=\"cr1\"]/@class");
            Assert.IsNotNull(classAttribute);
            Assert.AreEqual("workitem cr", classAttribute.Value);
        }

        [TestMethod]
        public void ShouldRenderFromLeafStep()
        {
            _workStepRepository.Add(WorkStep.New("/scheduled").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1","/scheduled"));

            var doc = GetDocument(_workStepRepository.GetWorkStep("/scheduled"));

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"cr1\"]"));
        }


        [TestMethod]
        public void ShouldRenderParallelStepsWithRightClass()
        {
            _workStepRepository.Add(WorkStep.New("/feedback").UpdateOrdinal(1).UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback/review").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-review"));

            var doc = GetDocument();

            Assert.AreEqual("workstep step-cr-review review", doc.SelectSingleNode("/html/body/ol/li[@id=\"feedback\"]/ul/li[position()=1]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderParalleledChildItemsWithRightClasses()
        {
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback").UpdateOrdinal(2).UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback/review").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-review"));
            _workStepRepository.Add(WorkStep.New("/feedback/test").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-test"));

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback"));

            var doc = GetDocument();

            Assert.AreEqual("workitem cr cr-test", doc.SelectSingleNode("//li[@id=\"development\"]/ol/li[@id=\"cr1-test\"]/@class").Value);
            Assert.AreEqual("workitem cr cr-review", doc.SelectSingleNode("//li[@id=\"feedback.review\"]/ol/li[@id=\"cr1-review\"]/@class").Value);
        }

        [TestMethod]
        public void ShouldNotRenderParallelLockedWorkItem()
        {
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback").UpdateOrdinal(2).UpdateType(WorkStepType.Parallel).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/feedback/review").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-review"));
            _workStepRepository.Add(WorkStep.New("/feedback/test").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-test"));

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback"));

            var doc = GetDocument();

            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"feedback\"]/ol/li[@id=\"cr1\"]"));
        }


        [TestMethod]
        public void ShouldRenderCorrectOuterExpandStepClass()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));

            var doc = GetDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li"));
            Assert.AreEqual("inprocess",doc.SelectSingleNode("//li[@id=\"development\"]/ol/li/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderCorrectClassForExpandTemplate()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));

            var doc = GetDocument();

            Assert.AreEqual("expand", doc.SelectSingleNode("//li[@id='development']/ol/li/ol/li/@class").Value);
        }


        [TestMethod]
        public void ShouldRenderWorkStepForWorkItemInExpandTemplate()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));

            var doc = GetDocument();

            Assert.AreEqual("workstep step-cr", doc.SelectSingleNode("//li[@class='expand']/ol/li[position()=1]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderFirstDescendantStepAfterWorkItemStepInExpandTemplate()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess/tasks").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("task"));

            var doc = GetDocument();

            Assert.IsNotNull(doc.SelectSingleNode("//li[@class='expand']/ol/li[position()=2]"));
            Assert.AreEqual("development.inprocess.tasks", doc.SelectSingleNode("//li[@class='expand']/ol/li[position()=2]/@id").Value);
            Assert.AreEqual("workstep step-task tasks", doc.SelectSingleNode("//li[@class='expand']/ol/li[position()=2]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderMultiLevelDescendantsStepAfterWorkItemStepInExpandTemplate()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess/tasks").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("task"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess/tasks/new").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("task"));

            var doc = GetDocument();

            Assert.AreEqual("development.inprocess.tasks.new", doc.SelectSingleNode("//li[@class='expand']/ol/li[@id='development.inprocess.tasks']/ol/li/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderTransientStepBeforeExpandStep()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem("/analysis", "cr1");
            _wp.MoveWorkItem("/development", "cr1");


            var doc = GetDocument();

            //Assert.AreEqual(string.Empty, doc.InnerXml);

            XmlNode developmentInProcess = doc.SelectSingleNode("//li[@id=\"development\"]/ol/li");

            Assert.AreEqual("transient", developmentInProcess.SelectSingleNode("ol/li[position()=1]/@class").Value);
            Assert.AreEqual("expand", developmentInProcess.SelectSingleNode("ol/li[position()=2]/@class").Value);
            Assert.IsNull(developmentInProcess.SelectSingleNode("ol/li[position()=3]/@class"));
        }

        [TestMethod]
        public void ShouldRenderTransientStepWithCorrectIdForWorkItemContainer()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem("/analysis", "cr1");
            _wp.MoveWorkItem("/development", "cr1");

            var doc = GetDocument();

            Assert.IsNull(doc.SelectSingleNode("//li[@class='transient']/@id"));
            Assert.AreEqual("development.inprocess.cr1", doc.SelectSingleNode("//li[@class='transient']/ol/li/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderTitleForChildOfTransientStep()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess/tasks").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("task").UpdateTitle("Tasks"));
            _workStepRepository.Add(WorkStep.New("/development/inprocess/tasks/new").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("task"));

            _wp.CreateWorkItem("/analysis", "cr1");
            _wp.MoveWorkItem("/development", "cr1");


            var doc = GetDocument();

            Assert.IsNotNull(doc.SelectSingleNode("//li[@class='transient']/ol/li[@class='tasks']/h1"));
        }

        [TestMethod]
        public void ShouldRenderWorkItemWithoutTimeStamp()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workItemRepository.CreateWorkItem(WorkItem.New("cr1","/analysis"));

            GetDocument();
        }

        [TestMethod]
        public void FullFeatureTest()
        {
            _workStepRepository.Add(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr").UpdateTitle("Analysis"));
            _workStepRepository.Add(WorkStep.New("/analysis/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
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
            _workStepRepository.Add(WorkStep.New("/feedback/demo").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-demo").UpdateTitle("Demo"));
            _workStepRepository.Add(WorkStep.New("/feedback/test").UpdateOrdinal(3).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr-test").UpdateTitle("Test"));
            _workStepRepository.Add(WorkStep.New("/done").UpdateOrdinal(4).UpdateType(WorkStepType.End).UpdateWorkItemClass("cr").UpdateTitle("Done"));

            _wp.CreateWorkItem("/analysis","cr1","cr2","cr3","cr4","cr5","cr6","cr7","cr8","cr9","cr10", "cr11", "cr12");
            _wp.MoveWorkItem("/analysis/done", "cr4");
            _wp.MoveWorkItem("/development/inprocess", "cr5", "cr6");
            _wp.CreateWorkItem("/development/inprocess/cr5", "cr5-1", "cr5-2", "cr5-3", "cr5-4");
            _wp.CreateWorkItem("/development/inprocess/cr6", "cr6-1", "cr6-2", "cr6-3", "cr6-4");
            _wp.MoveWorkItem("/development/inprocess/cr5/tasks/done", "cr5-1", "cr5-2");
            _wp.MoveWorkItem("/development/inprocess/cr6/tasks/inprocess", "cr6-1");
            _wp.MoveWorkItem("/development/done", "cr7", "cr8", "cr9", "cr10", "cr11", "cr12");
            _wp.MoveWorkItem("/feedback/review", "cr7", "cr8");
            _wp.MoveWorkItem("/feedback/demo", "cr9", "cr10");
            _wp.MoveWorkItem("/feedback/test", "cr11");
            _wp.MoveWorkItem("/done", "cr7-review");
            _wp.MoveWorkItem("/done", "cr9-demo");
            _wp.MoveWorkItem("/done", "cr12");

            AssertIsAsExpected(Resources.FullHtml);
        }

        private void AssertIsAsExpected(string expectedXml)
        {
            var expected = new XmlDocument();
            expected.LoadXml(expectedXml);

            var actual = GetDocument();
            Assert.AreEqual(expected.InnerXml, actual.InnerXml);
        }

        private XmlDocument GetDocument()
        {
            return GetDocument(WorkStep.Root);        
        }   

        private XmlDocument GetDocument(WorkStep workStep)
        {
            var htmlRenderer = new HtmlRenderer(_wp);

            var doc = new XmlDocument();
            using (var writeStream = new MemoryStream())
            {

                htmlRenderer.Render(writeStream, workStep);

                var readStream = new MemoryStream(writeStream.ToArray());
                doc.Load(readStream);
            }
            return doc;
        }

    }
}