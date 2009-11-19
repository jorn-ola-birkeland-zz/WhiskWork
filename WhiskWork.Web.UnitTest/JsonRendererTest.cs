using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Core;
using WhiskWork.Test.Common;
using WhiskWork.Web.UnitTest.Properties;
using System.Text;

namespace WhiskWork.Web.UnitTest
{
    [TestClass]
    public class JsonRendererTest
    {
        private Workflow _wp;
        private JsonRenderer _jsonRenderer;

        [TestInitialize]
        public void Init()
        {
            var workflowRepository = new MemoryWorkStepRepository();
            var workItemRepository = new MemoryWorkItemRepository();

            _jsonRenderer = new JsonRenderer(workflowRepository, workItemRepository);
            _wp = new Workflow(workflowRepository, workItemRepository);
        }


        [TestMethod]
        public void ShouldRenderSingleEmptyStep()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{workstep:\"analysis\",workitemList:[]}]",json);
        }

        [TestMethod]
        public void ShouldRenderTwoEmptySteps()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkStep(new WorkStep("/development", "/", 2, WorkStepType.Normal, "cr"));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{workstep:\"analysis\",workitemList:[]},{workstep:\"development\",workitemList:[]}]", json);
        }

        [TestMethod]
        public void ShouldRenderSingleStepWithOneWorkItem()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1","/analysis"));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{workstep:\"analysis\",workitemList:[{id:\"cr1\"}]}]", json);
        }

        [TestMethod]
        public void ShouldRenderSingleStepWithTwoWorkItems()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr2", "/analysis"));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{workstep:\"analysis\",workitemList:[{id:\"cr1\"},{id:\"cr2\"}]}]", json);
        }

        [TestMethod]
        public void ShouldRenderSingleStepWithOneWorkItemWithOneProperty()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis",new NameValueCollection {{"prop","value"}}));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{workstep:\"analysis\",workitemList:[{id:\"cr1\",prop:\"value\"}]}]", json);
        }

        [TestMethod]
        public void ShouldRenderNestedWorkSteps()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkStep(new WorkStep("/analysis/inprocess", "/analysis", 1, WorkStepType.Begin, "cr"));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{workstep:\"analysis\",workitemList:[]},{workstep:\"analysis-inprocess\",workitemList:[]}]", json);
        }


        [TestMethod]
        public void ShouldRenderExpandedWorkItemWithOneChildWorkItem()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkStep(new WorkStep("/development","/",2, WorkStepType.Expand,"cr"));
            _wp.CreateWorkStep(new WorkStep("/development/new", "/development", 1, WorkStepType.Begin, "task"));
            
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));
            _wp.CreateWorkItem(WorkItem.New("cr1-1", "/development/cr1/new"));

            var json = GetJson(WorkStep.Root);

            const string expected = "[{workstep:\"analysis\",workitemList:[]},{workstep:\"development\",workitemList:[{id:\"cr1\",worksteps:[{workstep:\"development-cr1-new\",workitemList:[{id:\"cr1-1\"}]}]}]}]";
            Assert.AreEqual(expected, json);
        }

        [TestMethod]
        public void ShouldRenderTwoWorkItemsinExpandStep()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkStep(new WorkStep("/development", "/", 2, WorkStepType.Expand, "cr"));
            _wp.CreateWorkStep(new WorkStep("/development/new", "/development", 1, WorkStepType.Begin, "task"));

            _wp.Create("/analysis","cr1","cr2");
            _wp.Move("/development","cr1","cr2");

            var json = GetJson(WorkStep.Root);

            const string expected = "[{workstep:\"analysis\",workitemList:[]},{workstep:\"development\",workitemList:[{id:\"cr1\",worksteps:[{workstep:\"development-cr1-new\",workitemList:[]}]},{id:\"cr2\",worksteps:[{workstep:\"development-cr2-new\",workitemList:[]}]}]}]";
            Assert.AreEqual(expected, json);
        }

        [TestMethod]
        public void ShouldSortAccordingToOrdinal()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));

            _wp.Create("/analysis", "cr1", "cr2");
            _wp.UpdateOrdinal("cr1", 2);
            _wp.UpdateOrdinal("cr2", 1);

            var json = GetJson(WorkStep.Root);

            const string expected = "[{workstep:\"analysis\",workitemList:[{id:\"cr2\"},{id:\"cr1\"}]}]";
            Assert.AreEqual(expected, json);

        }

        [TestMethod]
        public void ShouldEscapeQuotesInProperties()
        {
            _wp.CreateWorkStep(new WorkStep("/analysis", "/", 1, WorkStepType.Begin, "cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis", new NameValueCollection { { "prop", "va\"l\"ue" } }));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{workstep:\"analysis\",workitemList:[{id:\"cr1\",prop:\"va\\\"l\\\"ue\"}]}]", json);
   
        }

        private string GetJson(WorkStep workStep)
        {
            
            MemoryStream readStream;

            using (var writeStream = new MemoryStream())
            {

                _jsonRenderer.Render(writeStream, workStep);
                readStream = new MemoryStream(writeStream.ToArray());
            }

            using (var reader = new StreamReader(readStream))
            {
                return reader.ReadToEnd();
            }

        }

    }
}
