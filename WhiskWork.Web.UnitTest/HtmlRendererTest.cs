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
            _wp = new Workflow(_workStepRepository, _workItemRepository);

        }
            

        [TestMethod]
        public void ShouldRenderSingleStepWorkflow()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");

            var doc = GetDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]"));
        }

        [TestMethod]
        public void ShouldRenderSingleStepWithTitleWorkflow()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");

            var doc = GetDocument();

            Assert.AreEqual("Analysis", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/h1").InnerText.Trim());
        }


        [TestMethod]
        public void ShouldRenderStepsInRightOrder()
        {
            _workStepRepository.Add("/test", "/", 3, WorkStepType.End, "cr", "Test");
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr", "Development");

            var doc = GetDocument();

            Assert.AreEqual("analysis",doc.SelectSingleNode("/html/body/ol/li[position()=1]/@id").Value);
            Assert.AreEqual("development", doc.SelectSingleNode("/html/body/ol/li[position()=2]/@id").Value);
            Assert.AreEqual("test", doc.SelectSingleNode("/html/body/ol/li[position()=3]/@id").Value);


        }

        [TestMethod]
        public void ShouldRenderSubSteps()
        {
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr", "Development ");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Normal, "cr", "In process");
            _workStepRepository.Add("/development/done", "/development", 2, WorkStepType.Normal, "cr", "Dev. done");

            var doc = GetDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li[@id=\"development.inprocess\"]"));
            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li[@id=\"development.done\"]"));
        }

        [TestMethod]
        public void ShouldRenderClasses()
        {
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr", "Development ");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Normal, "cr", "In process");
            _workStepRepository.Add("/development/done", "/development", 2, WorkStepType.Normal, "cr", "Dev. done");

            var doc = GetDocument();

            Assert.AreEqual("workstep step-cr inprocess", doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderSingleWorkItemInSingleStepWorkflow()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));

            var doc = GetDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li[@id=\"cr1\"]"));
        }

        [TestMethod]
        public void ShouldRenderSingleWorkItemProperty()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis",new NameValueCollection {{"Name","CR1"}}));

            var doc = GetDocument();

            Assert.AreEqual("Name", doc.SelectSingleNode("//li[@id=\"cr1\"]/dl/dt[@class='name']").InnerText);
            Assert.AreEqual("CR1", doc.SelectSingleNode("//li[@id=\"cr1\"]/dl/dd[@class='name']").InnerText);
        }

        [TestMethod]
        public void ShouldRenderTwoWorkItemProperties()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
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
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis",new NameValueCollection { { "Name", "value" } }));

            var doc = GetDocument();

            Assert.AreEqual("name", doc.SelectSingleNode("//li[@id=\"cr1\"]/dl/dt/@class").InnerText);
        }


        [TestMethod]
        public void ShouldHtmlEncodePropertyValues()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis",new NameValueCollection { { "name", "&<> " } }));

            var doc = GetDocument();

            Assert.AreEqual("&<> ", doc.SelectSingleNode("//li[@id=\"cr1\"]/dl/dd[@class='name']").InnerText);
        }

        [TestMethod]
        public void ShouldHtmlEncodeTitle()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "<Analysis & Test>");

            var doc = GetDocument();

            Assert.AreEqual("<Analysis & Test>", doc.SelectSingleNode("//h1").InnerText.Trim());
        }


        [TestMethod]
        public void ShouldRenderTwoWorkItemsInSingleStepWorkflow()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr2","/analysis"));

            var doc = GetDocument();

            Assert.AreEqual("cr1", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li[position()=1]/@id").Value);
            Assert.AreEqual("cr2", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li[position()=2]/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderAWorkItemInEachStepForAThreeStepWorkflow()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr");
            _workStepRepository.Add("/done", "/", 3, WorkStepType.End, "cr");
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
            _workStepRepository.Add("/feedback", "/", 1, WorkStepType.Parallel, "cr");
            _workStepRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr");
            _workStepRepository.Add("/feedback/test", "/feedback", 2, WorkStepType.Normal, "cr-test");

            var doc = GetDocument();

            Assert.AreEqual("feedback.review", doc.SelectSingleNode("/html/body/ol/li[@id=\"feedback\"]/ul/li[position()=1]/@id").Value);
            Assert.AreEqual("feedback.test", doc.SelectSingleNode("/html/body/ol/li[@id=\"feedback\"]/ul/li[position()=2]/@id").Value);
        }


        [TestMethod]
        public void ShouldNotRenderEmptyListTagInLeafStepWithNoWorkItems()
        {
            _workStepRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");

            var doc = GetDocument();

            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"development\"]/ol"));
            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"development\"]/ul"));
        }

        [TestMethod]
        public void ShouldNotRenderH1TagIfWorkStepTitleIsEmpty()
        {
            _workStepRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");

            var doc = GetDocument();

            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"development\"]/h1"));
        }


        [TestMethod]
        public void ShouldRenderWorkItemWithRightClasses()
        {
            _workStepRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");
            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));

            var doc = GetDocument();

            var classAttribute = doc.SelectSingleNode("//li[@id=\"development\"]/ol/li[@id=\"cr1\"]/@class");
            Assert.IsNotNull(classAttribute);
            Assert.AreEqual("workitem cr", classAttribute.Value);
        }

        [TestMethod]
        public void ShouldRenderFromLeafStep()
        {
            _workStepRepository.Add("/scheduled", "/", 1, WorkStepType.Begin, "cr");
            _wp.CreateWorkItem(WorkItem.New("cr1","/scheduled"));

            var doc = GetDocument(_workStepRepository.GetWorkStep("/scheduled"));

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"cr1\"]"));
        }


        [TestMethod]
        public void ShouldRenderParallelStepsWithRightClass()
        {
            _workStepRepository.Add("/feedback", "/", 1, WorkStepType.Parallel, "cr");
            _workStepRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr-review");

            var doc = GetDocument();

            Assert.AreEqual("workstep step-cr-review review", doc.SelectSingleNode("/html/body/ol/li[@id=\"feedback\"]/ul/li[position()=1]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderParalleledChildItemsWithRightClasses()
        {
            _workStepRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/feedback", "/", 2, WorkStepType.Parallel, "cr");
            _workStepRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr-review");
            _workStepRepository.Add("/feedback/test", "/feedback", 2, WorkStepType.Normal, "cr-test");

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback"));

            var doc = GetDocument();

            Assert.AreEqual("workitem cr cr-test", doc.SelectSingleNode("//li[@id=\"development\"]/ol/li[@id=\"cr1-test\"]/@class").Value);
            Assert.AreEqual("workitem cr cr-review", doc.SelectSingleNode("//li[@id=\"feedback.review\"]/ol/li[@id=\"cr1-review\"]/@class").Value);
        }

        [TestMethod]
        public void ShouldNotRenderParallelLockedWorkItem()
        {
            _workStepRepository.Add("/development", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/feedback", "/", 2, WorkStepType.Parallel, "cr");
            _workStepRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr-review");
            _workStepRepository.Add("/feedback/test", "/feedback", 2, WorkStepType.Normal, "cr-test");

            _wp.CreateWorkItem(WorkItem.New("cr1","/development"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/feedback"));

            var doc = GetDocument();

            Assert.IsNull(doc.SelectSingleNode("//li[@id=\"feedback\"]/ol/li[@id=\"cr1\"]"));
        }


        [TestMethod]
        public void ShouldRenderCorrectOuterExpandStepClass()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");

            var doc = GetDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li"));
            Assert.AreEqual("inprocess",doc.SelectSingleNode("//li[@id=\"development\"]/ol/li/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderCorrectClassForExpandTemplate()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");

            var doc = GetDocument();

            Assert.AreEqual("expand", doc.SelectSingleNode("//li[@id='development']/ol/li/ol/li/@class").Value);
        }


        [TestMethod]
        public void ShouldRenderWorkStepForWorkItemInExpandTemplate()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");

            var doc = GetDocument();

            Assert.AreEqual("workstep step-cr", doc.SelectSingleNode("//li[@class='expand']/ol/li[position()=1]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderFirstDescendantStepAfterWorkItemStepInExpandTemplate()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workStepRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal, "task");

            var doc = GetDocument();

            Assert.IsNotNull(doc.SelectSingleNode("//li[@class='expand']/ol/li[position()=2]"));
            Assert.AreEqual("development.inprocess.tasks", doc.SelectSingleNode("//li[@class='expand']/ol/li[position()=2]/@id").Value);
            Assert.AreEqual("workstep step-task tasks", doc.SelectSingleNode("//li[@class='expand']/ol/li[position()=2]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderMultiLevelDescendantsStepAfterWorkItemStepInExpandTemplate()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workStepRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal, "task");
            _workStepRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1, WorkStepType.Begin, "task");

            var doc = GetDocument();

            Assert.AreEqual("development.inprocess.tasks.new", doc.SelectSingleNode("//li[@class='expand']/ol/li[@id='development.inprocess.tasks']/ol/li/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderTransientStepBeforeExpandStep()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");

            _wp.Create("/analysis", "cr1");
            _wp.Move("/development", "cr1");


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
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");

            _wp.Create("/analysis", "cr1");
            _wp.Move("/development", "cr1");

            var doc = GetDocument();

            Assert.IsNull(doc.SelectSingleNode("//li[@class='transient']/@id"));
            Assert.AreEqual("development.inprocess.cr1", doc.SelectSingleNode("//li[@class='transient']/ol/li/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderTitleForChildOfTransientStep()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workStepRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal, "task", "Tasks");
            _workStepRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1, WorkStepType.Begin, "task");

            _wp.Create("/analysis", "cr1");
            _wp.Move("/development", "cr1");


            var doc = GetDocument();

            Assert.IsNotNull(doc.SelectSingleNode("//li[@class='transient']/ol/li[@class='tasks']/h1"));
        }

        [TestMethod]
        public void FullFeatureTest()
        {
            _workStepRepository.Add("/analysis", "/", 1, WorkStepType.Normal, "cr", "Analysis");
            _workStepRepository.Add("/analysis/inprocess", "/analysis", 1, WorkStepType.Begin, "cr");
            _workStepRepository.Add("/analysis/done", "/analysis", 1, WorkStepType.Normal, "cr");
            _workStepRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr", "Development");
            _workStepRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workStepRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal, "task", "Tasks");
            _workStepRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1, WorkStepType.Begin, "task");
            _workStepRepository.Add("/development/inprocess/tasks/inprocess", "/development/inprocess/tasks", 1, WorkStepType.Normal, "task");
            _workStepRepository.Add("/development/inprocess/tasks/done", "/development/inprocess/tasks", 1, WorkStepType.End, "task");
            _workStepRepository.Add("/development/done", "/development", 2, WorkStepType.End, "cr");
            _workStepRepository.Add("/feedback", "/", 3, WorkStepType.Parallel, "cr");
            _workStepRepository.Add("/feedback/review", "/feedback", 1, WorkStepType.Normal, "cr-review", "Review");
            _workStepRepository.Add("/feedback/demo", "/feedback", 2, WorkStepType.Normal, "cr-demo", "Demo");
            _workStepRepository.Add("/feedback/test", "/feedback", 3, WorkStepType.Normal, "cr-test", "Test");
            _workStepRepository.Add("/done", "/", 4, WorkStepType.End, "cr", "Done");

            _wp.Create("/analysis","cr1","cr2","cr3","cr4","cr5","cr6","cr7","cr8","cr9","cr10", "cr11", "cr12");
            _wp.Move("/analysis/done", "cr4");
            _wp.Move("/development/inprocess", "cr5", "cr6");
            _wp.Create("/development/inprocess/cr5", "cr5-1", "cr5-2", "cr5-3", "cr5-4");
            _wp.Create("/development/inprocess/cr6", "cr6-1", "cr6-2", "cr6-3", "cr6-4");
            _wp.Move("/development/inprocess/cr5/tasks/done", "cr5-1", "cr5-2");
            _wp.Move("/development/inprocess/cr6/tasks/inprocess", "cr6-1");
            _wp.Move("/development/done", "cr7", "cr8", "cr9", "cr10", "cr11", "cr12");
            _wp.Move("/feedback/review", "cr7", "cr8");
            _wp.Move("/feedback/demo", "cr9", "cr10");
            _wp.Move("/feedback/test", "cr11");
            _wp.Move("/done", "cr7-review");
            _wp.Move("/done", "cr9-demo");
            _wp.Move("/done", "cr12");

            AssertIsAsExpected(Resources.FullFeatureTest);
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
            var htmlRenderer = new HtmlRenderer(_workStepRepository, _workItemRepository);

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