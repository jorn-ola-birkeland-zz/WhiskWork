using System;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Core;
using System.IO;
using WhiskWork.Web;

namespace WhiskWork.UnitTest
{
    [TestClass]
    public class HtmlRenderTest
    {
        private TestWorkflowRepository _workflowRepository;
        private TestWorkItemRepository _workItemRepository;
        private Workflow _wp;

        [TestInitialize]
        public void Init()
        {
            _workflowRepository = new TestWorkflowRepository();
            _workItemRepository = new TestWorkItemRepository();
            _wp = new Workflow(_workflowRepository, _workItemRepository);

        //    _workflowRepository.Add("/analysis", null, 1, WorkStepType.Begin, "cr");
        //    _workflowRepository.Add("/development", null, 2, WorkStepType.Normal, "cr");
        //    _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
        //    _workflowRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal,
        //                            "task");
        //    _workflowRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1,
        //                            WorkStepType.Begin, "task");
        //    _workflowRepository.Add("/development/inprocess/tasks/inprocess", "/development/inprocess/tasks", 2,
        //                            WorkStepType.Normal, "task");
        //    _workflowRepository.Add("/development/inprocess/tasks/done", "/development/inprocess/tasks", 3,
        //                            WorkStepType.End, "task");
        //    _workflowRepository.Add("/development/done", "/development", 2, WorkStepType.Normal, "cr");
        //    _workflowRepository.Add("/done", null, 1, WorkStepType.End, "cr");
        }
            

        [TestMethod]
        public void ShouldRenderSingleStepWorkflow()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");

            XmlDocument doc = GetFullDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]"));
        }

        [TestMethod]
        public void ShouldRenderSingleStepWithTitleWorkflow()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");

            XmlDocument doc = GetFullDocument();

            Assert.AreEqual("Analysis", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/h1").InnerText.Trim());
        }


        [TestMethod]
        public void ShouldRenderStepsInRightOrder()
        {
            _workflowRepository.Add("/test", "/", 3, WorkStepType.End, "cr", "Test");
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr", "Development");

            XmlDocument doc = GetFullDocument();

            Assert.AreEqual("analysis",doc.SelectSingleNode("/html/body/ol/li[position()=1]/@id").Value);
            Assert.AreEqual("development", doc.SelectSingleNode("/html/body/ol/li[position()=2]/@id").Value);
            Assert.AreEqual("test", doc.SelectSingleNode("/html/body/ol/li[position()=3]/@id").Value);


        }

        [TestMethod]
        public void ShouldRenderSubSteps()
        {
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr", "Development ");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Normal, "cr", "In process");
            _workflowRepository.Add("/development/done", "/development", 2, WorkStepType.Normal, "cr", "Dev. done");

            XmlDocument doc = GetFullDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li[@id=\"development.inprocess\"]"));
            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"development\"]/ol/li[@id=\"development.done\"]"));
        }

        [TestMethod]
        public void ShouldRenderClasses()
        {
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Normal, "cr", "Development ");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Normal, "cr", "In process");
            _workflowRepository.Add("/development/done", "/development", 2, WorkStepType.Normal, "cr", "Dev. done");

            XmlDocument doc = GetFullDocument();

            Assert.AreEqual("workstep step-cr inprocess", doc.SelectSingleNode("//li[@id=\"development.inprocess\"]/@class").Value);
        }

        [TestMethod]
        public void ShouldRenderSingleWorkItemInSingleStepWorkflow()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
            _wp.CreateWorkItem("cr1","/analysis");

            XmlDocument doc = GetFullDocument();

            Assert.IsNotNull(doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li[@id=\"cr1\"]"));
        }

        [TestMethod]
        public void ShouldRenderTwoWorkItemsInSingleStepWorkflow()
        {
            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr", "Analysis");
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.CreateWorkItem("cr2", "/analysis");

            XmlDocument doc = GetFullDocument();

            Assert.AreEqual("cr1", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li[position()=1]/@id").Value);
            Assert.AreEqual("cr2", doc.SelectSingleNode("/html/body/ol/li[@id=\"analysis\"]/ol/li[position()=2]/@id").Value);
        }


        private XmlDocument GetFullDocument()
        {
            var htmlRenderer = new HtmlRenderer(_workflowRepository, _workItemRepository);

            var doc = new XmlDocument();
            using (var writeStream = new MemoryStream())
            {
                
                htmlRenderer.RenderFull(writeStream);

                var readStream = new MemoryStream(writeStream.ToArray());
                doc.Load(readStream);
            }
            return doc;
        }
    }
}
