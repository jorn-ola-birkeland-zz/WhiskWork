using System;
using System.Collections.Specialized;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Core;
using WhiskWork.Test.Common;

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
            var workStepRepository = new MemoryWorkStepRepository();
            var workItemRepository = new MemoryWorkItemRepository();

            var workflowRepository = new WorkflowRepository(workItemRepository, workStepRepository);
            _wp = new Workflow(workflowRepository);
            _jsonRenderer = new JsonRenderer(_wp);
        }


        [TestMethod]
        public void ShouldRenderSingleEmptyStep()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{\"workstep\":\"analysis\",\"workitemList\":[]}]", json);
        }

        [TestMethod]
        public void ShouldRenderTwoEmptySteps()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkStep(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Normal).UpdateWorkItemClass("cr"));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{\"workstep\":\"analysis\",\"workitemList\":[]},{\"workstep\":\"development\",\"workitemList\":[]}]", json);
        }

        [TestMethod]
        public void ShouldRenderSingleStepWithOneWorkItem()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{\"workstep\":\"analysis\",\"workitemList\":[{\"id\":\"cr1\"}]}]", json);
        }

        [TestMethod]
        public void ShouldRenderSingleStepWithTwoWorkItems()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            _wp.CreateWorkItem(WorkItem.New("cr2", "/analysis"));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{\"workstep\":\"analysis\",\"workitemList\":[{\"id\":\"cr1\"},{\"id\":\"cr2\"}]}]", json);
        }

        [TestMethod]
        public void ShouldRenderSingleStepWithOneWorkItemWithOneProperty()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis", new NameValueCollection { { "prop", "value" } }));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{\"workstep\":\"analysis\",\"workitemList\":[{\"id\":\"cr1\",\"prop\":\"value\"}]}]", json);
        }

        [TestMethod]
        public void ShouldRenderNestedWorkSteps()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkStep(WorkStep.New("/analysis/inprocess").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{\"workstep\":\"analysis\",\"workitemList\":[]},{\"workstep\":\"analysis-inprocess\",\"workitemList\":[]}]", json);
        }


        [TestMethod]
        public void ShouldRenderExpandedWorkItemWithOneChildWorkItem()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkStep(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));
            _wp.CreateWorkStep(WorkStep.New("/development/new").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("task"));
            
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            _wp.UpdateWorkItem(WorkItem.New("cr1", "/development"));
            _wp.CreateWorkItem(WorkItem.New("cr1-1", "/development/cr1/new"));

            var json = GetJson(WorkStep.Root);

            const string expected = "[{\"workstep\":\"analysis\",\"workitemList\":[]},{\"workstep\":\"development\",\"workitemList\":[{\"id\":\"cr1\",\"worksteps\":[{\"workstep\":\"development-cr1-new\",\"workitemList\":[{\"id\":\"cr1-1\"}]}]}]}]";
            Assert.AreEqual(expected, json);
        }

        [TestMethod]
        public void ShouldRenderTwoWorkItemsinExpandStep()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkStep(WorkStep.New("/development").UpdateOrdinal(2).UpdateType(WorkStepType.Expand).UpdateWorkItemClass("cr"));
            _wp.CreateWorkStep(WorkStep.New("/development/new").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("task"));

            _wp.CreateWorkItem("/analysis","cr1","cr2");
            _wp.MoveWorkItem("/development","cr1","cr2");

            var json = GetJson(WorkStep.Root);

            const string expected = "[{\"workstep\":\"analysis\",\"workitemList\":[]},{\"workstep\":\"development\",\"workitemList\":[{\"id\":\"cr1\",\"worksteps\":[{\"workstep\":\"development-cr1-new\",\"workitemList\":[]}]},{\"id\":\"cr2\",\"worksteps\":[{\"workstep\":\"development-cr2-new\",\"workitemList\":[]}]}]}]";
            Assert.AreEqual(expected, json);
        }

        [TestMethod]
        public void ShouldSortAccordingToOrdinal()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            _wp.CreateWorkItem("/analysis", "cr1", "cr2");
            _wp.UpdateOrdinal("cr1", 2);
            _wp.UpdateOrdinal("cr2", 1);

            var json = GetJson(WorkStep.Root);

            const string expected = "[{\"workstep\":\"analysis\",\"workitemList\":[{\"id\":\"cr2\"},{\"id\":\"cr1\"}]}]";
            Assert.AreEqual(expected, json);

        }

        [TestMethod]
        public void ShouldEscapeQuotesInProperties()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));
            _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis", new NameValueCollection { { "prop", "va\"l\"ue" } }));

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{\"workstep\":\"analysis\",\"workitemList\":[{\"id\":\"cr1\",\"prop\":\"va\\u0022l\\u0022ue\"}]}]", json);
   
        }

        [TestMethod, Ignore]
        public void ShouldRenderTimeStamp()
        {
            _wp.CreateWorkStep(WorkStep.New("/analysis").UpdateOrdinal(1).UpdateType(WorkStepType.Begin).UpdateWorkItemClass("cr"));

            var mocks = new MockRepository();
            var expectedTime = new DateTime(2010,11,26,14,35,11,323);

            _wp.MockTime(mocks,expectedTime);
            
            using(mocks.Playback())
            {
                _wp.CreateWorkItem(WorkItem.New("cr1", "/analysis"));
            }

            var json = GetJson(WorkStep.Root);

            Assert.AreEqual("[{\"workstep\":\"analysis\",\"workitemList\":[{\"id\":\"cr1\",\"timestamp\":\"2010-11-26T14:35:11.323\"}]}]", json);

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
