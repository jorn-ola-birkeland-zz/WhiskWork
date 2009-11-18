using System.Collections.Specialized;
using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Core;
using WhiskWork.Test.Common;

namespace WhiskWork.Web.UnitTest
{
    [TestClass]
    public class XmlRendererTest
    {
        private Workflow _wp;
        private XmlRenderer _xmlRenderer;

        [TestInitialize]
        public void Init()
        {
            var workflowRepository = new MemoryWorkflowRepository();
            var workItemRepository = new MemoryWorkItemRepository();

            _xmlRenderer = new XmlRenderer(workflowRepository, workItemRepository);
            _wp = new Workflow(workflowRepository, workItemRepository);
        }


        [TestMethod]
        public void ShouldRenderSingleEmptyStep()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));

            var doc = GetDocument();

            Assert.AreEqual("analysis",doc.SelectSingleNode("/WorkSteps/WorkStep/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderWorkStepWithWorkItemClass()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));

            var doc = GetDocument();

            Assert.AreEqual("cr", doc.SelectSingleNode("/WorkSteps/WorkStep[@id='analysis']/@workItemClass").Value);
        }

        [TestMethod]
        public void ShouldRenderTwoEmptySteps()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkStep(new WorkStep("/development", "/", 2, WorkStepType.Begin, "cr"));

            var doc = GetDocument();

            Assert.AreEqual("analysis", doc.SelectSingleNode("/WorkSteps/WorkStep[position()=1]/@id").Value);
            Assert.AreEqual("development", doc.SelectSingleNode("/WorkSteps/WorkStep[position()=2]/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderSingleStepWithOneWorkItem()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.Create("/analysis", "cr1");

            var doc = GetDocument();

            Assert.AreEqual("cr1", doc.SelectSingleNode("/WorkSteps/WorkStep/WorkItems/WorkItem/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderStepWithOneChildStep()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkStep(new WorkStep("/analysis/inprocess", "/analysis", 1, WorkStepType.Begin, "cr"));

            var doc = GetDocument();

            Assert.AreEqual("analysis.inprocess", doc.SelectSingleNode("/WorkSteps/WorkStep[@id='analysis']/WorkSteps/WorkStep/@id").Value);
        }

        [TestMethod]
        public void ShouldRenderWorkItemWithSingleProperty()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis", new NameValueCollection { { "prop", "value" } }));

            var doc = GetDocument();

            Assert.AreEqual("value", doc.SelectSingleNode("//WorkItem[@id='cr1']/Properties/Property[@name='prop']").InnerText);
        }

        [TestMethod]
        public void ShouldRenderWorkItemWithOrdinal()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis").UpdateOrdinal(1));

            var doc = GetDocument();

            Assert.AreEqual("1", doc.SelectSingleNode("//WorkItem[@id='cr1']/@ordinal").Value);
        }

        [TestMethod]
        public void ShouldRenderWorkItemWithClasses()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));

            var doc = GetDocument();

            Assert.AreEqual("cr", doc.SelectSingleNode("//WorkItem[@id='cr1']/@classes").Value);
        }


        private XmlDocument GetDocument()
        {
            return GetDocument(WorkStep.Root);
        }

        private XmlDocument GetDocument(WorkStep workStep)
        {
            var doc = new XmlDocument();
            using (var writeStream = new MemoryStream())
            {

                _xmlRenderer.Render(writeStream, workStep);

                var readStream = new MemoryStream(writeStream.ToArray());
                doc.Load(readStream);
            }
            return doc;
        }

    }
}
