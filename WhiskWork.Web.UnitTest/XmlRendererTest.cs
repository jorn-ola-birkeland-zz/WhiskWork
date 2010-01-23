using System;
using System.Collections.Specialized;
using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Core;
using WhiskWork.Test.Common;

namespace WhiskWork.Web.UnitTest
{
    [TestClass]
    public class XmlRendererTest
    {
        private Workflow _wp;
        private XmlRenderer _xmlRenderer;
        private MemoryWorkItemRepository _workItemRepository;

        [TestInitialize]
        public void Init()
        {
            var workStepRepository = new MemoryWorkStepRepository();
            _workItemRepository = new MemoryWorkItemRepository();

            var workflowRepository = new WorkflowRepository(_workItemRepository, workStepRepository);
            _wp = new Workflow(workflowRepository);
            _xmlRenderer = new XmlRenderer(_wp);
        }

        [TestMethod]
        public void ShouldRenderSingleEmptyStep()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            var doc = _xmlRenderer.RenderToXmlDocument();

            Assert.AreEqual("analysis",doc.SelectSingleNode("/WorkSteps/WorkStep/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderWorkStepWithWorkItemClass()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            var doc = _xmlRenderer.RenderToXmlDocument();

            Assert.AreEqual("cr", doc.SelectSingleNode("/WorkSteps/WorkStep[@id='analysis']/@workItemClass").Value);
        }

        [TestMethod]
        public void ShouldRenderTwoEmptySteps()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkStep(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            var doc = _xmlRenderer.RenderToXmlDocument();

            Assert.AreEqual("analysis", doc.SelectSingleNode("/WorkSteps/WorkStep[position()=1]/@id").Value);
            Assert.AreEqual("development", doc.SelectSingleNode("/WorkSteps/WorkStep[position()=2]/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderSingleStepWithOneWorkItem()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem("/analysis", "cr1");

            var doc = _xmlRenderer.RenderToXmlDocument();

            Assert.AreEqual("cr1", doc.SelectSingleNode("/WorkSteps/WorkStep/WorkItems/WorkItem/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderStepWithOneChildStep()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkStep(WorkStep.New("/analysis/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            var doc = _xmlRenderer.RenderToXmlDocument();

            Assert.AreEqual("analysis.inprocess", doc.SelectSingleNode("/WorkSteps/WorkStep[@id='analysis']/WorkSteps/WorkStep/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderWorkItemWithSingleProperty()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis", new NameValueCollection { { "prop", "value" } }));

            var doc = _xmlRenderer.RenderToXmlDocument();

            Assert.AreEqual("value", doc.SelectSingleNode("//WorkItem[@id='cr1']/Properties/Property[@name='prop']").InnerText);
        }

        [TestMethod]
        public void ShouldRenderWorkItemWithOrdinal()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis").UpdateOrdinal(1));

            var doc = _xmlRenderer.RenderToXmlDocument();

            Assert.AreEqual("1", doc.SelectSingleNode("//WorkItem[@id='cr1']/@ordinal").Value);
        }

        [TestMethod]
        public void ShouldRenderWorkItemWithClasses()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));

            var doc = _xmlRenderer.RenderToXmlDocument();

            Assert.AreEqual("cr", doc.SelectSingleNode("//WorkItem[@id='cr1']/@classes").Value);
        }

        [TestMethod]
        public void ShouldRenderWorkItemWithTimeStamp()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            var mocks = new MockRepository();
            var expectedTime = DateTime.Now;
            var xmlExpectedTime = XmlConvert.ToString(expectedTime, XmlDateTimeSerializationMode.RoundtripKind);

            _wp.MockTime(mocks, expectedTime);

            using (mocks.Playback())
            {
                _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            }
            
            var doc = _xmlRenderer.RenderToXmlDocument();

            Assert.AreEqual(xmlExpectedTime, doc.SelectSingleNode("//WorkItem[@id='cr1']/@timestamp").Value);
        }

        [TestMethod]
        public void ShouldRenderWorkItemWithLastMoved()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkStep(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));

            var mocks = new MockRepository();
            var expectedTime = DateTime.Now;
            var xmlExpectedTime = XmlConvert.ToString(expectedTime, XmlDateTimeSerializationMode.RoundtripKind);

            _wp.MockTime(mocks, expectedTime);

            using (mocks.Playback())
            {
                _wp.CreateWorkItem("/analysis","cr1");
                _wp.MoveWorkItem("/development", "cr1");
            }

            var doc = _xmlRenderer.RenderToXmlDocument();

            Assert.AreEqual(xmlExpectedTime, doc.SelectSingleNode("//WorkItem[@id='cr1']/@lastmoved").Value);
        }


        [TestMethod]
        public void ShouldRenderMinimumWorkItem()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _workItemRepository.CreateWorkItem(WorkItem.New("cr1", "/analysis"));

            var doc = _xmlRenderer.RenderToXmlDocument();
            Assert.AreEqual("cr1", doc.SelectSingleNode("/WorkSteps/WorkStep/WorkItems/WorkItem/@id").Value);
        }
    }

    internal static class XmlRendererExtensions
    {
        public static XmlDocument RenderToXmlDocument(this XmlRenderer xmlRenderer)
        {
            return RenderToXmlDocument(xmlRenderer, WorkStep.Root);
        }

        public static XmlDocument RenderToXmlDocument(this XmlRenderer xmlRenderer, WorkStep workStep)
        {
            var doc = new XmlDocument();
            using (var writeStream = new MemoryStream())
            {

                xmlRenderer.Render(writeStream, workStep);

                var readStream = new MemoryStream(writeStream.ToArray());
                doc.Load(readStream);
            }
            return doc;
        }
    }
}
