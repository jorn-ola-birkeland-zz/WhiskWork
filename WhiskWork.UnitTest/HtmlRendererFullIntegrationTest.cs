using System;
using System.Collections.Specialized;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Core;
using System.IO;
using WhiskWork.Web;
using WhiskWork.UnitTest.Properties;

namespace WhiskWork.UnitTest
{
    [TestClass]
    public class HtmlRendererExpandIntegrationTest
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

            _workflowRepository.Add("/analysis", "/", 1, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development", "/", 2, WorkStepType.Begin, "cr");
            _workflowRepository.Add("/development/inprocess", "/development", 1, WorkStepType.Expand, "cr");
            _workflowRepository.Add("/development/inprocess/tasks", "/development/inprocess", 1, WorkStepType.Normal, "task");
            _workflowRepository.Add("/development/inprocess/tasks/new", "/development/inprocess/tasks", 1, WorkStepType.Begin, "task");
            _workflowRepository.Add("/development/inprocess/tasks/inprogress", "/development/inprocess/tasks", 1, WorkStepType.Normal, "task");
            _workflowRepository.Add("/development/inprocess/tasks/done", "/development/inprocess/tasks", 1, WorkStepType.End, "task");
            _workflowRepository.Add("/development/done", "/development", 2, WorkStepType.End, "cr");
            _workflowRepository.Add("/test", "/", 2, WorkStepType.Normal, "cr");
            _workflowRepository.Add("/done", "/", 2, WorkStepType.End, "cr");
        }
            

        [TestMethod]
        public void ExpandIntegrationTest1()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());

            AssertIsAsExpected(Resources.ExpandIntegarationTest1);
        }

        [TestMethod]
        public void ExpandIntegrationTest2()
        {
            _wp.CreateWorkItem("cr1", "/analysis");
            _wp.CreateWorkItem("cr2", "/analysis");
            _wp.UpdateWorkItem("cr1", "/development", new NameValueCollection());
            _wp.UpdateWorkItem("cr2", "/development", new NameValueCollection());

            AssertIsAsExpected(Resources.ExpandIntegarationTest2);
        }


        private void AssertIsAsExpected(string expectedXml)
        {
            var expected = new XmlDocument();
            expected.LoadXml(expectedXml);

            var actual = GetFullDocument();
            Assert.AreEqual(expected.InnerXml,actual.InnerXml);
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
