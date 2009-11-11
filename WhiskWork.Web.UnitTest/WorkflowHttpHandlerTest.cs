using System;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Core;

namespace WhiskWork.Web.UnitTest
{
    [TestClass]
    public class WorkflowHttpHandlerTest
    {
        private WorkflowHttpHandler _httpHandler;
        private MockRepository _mocks;
        private IWorkflow _workflow;
        private IWorkStepRendererFactory _rendererFactory;

        [TestInitialize]
        public void Init()
        {
            _mocks = new MockRepository();
            _workflow = _mocks.DynamicMock<IWorkflow>();
            _rendererFactory = _mocks.DynamicMock<IWorkStepRendererFactory>();

            _httpHandler = new WorkflowHttpHandler(_workflow, _rendererFactory);
        }

        [TestMethod]
        public void ShouldCreateWorkItemWhenPostingNewId()
        {
            using (_mocks.Record())
            {
                _workflow.CreateWorkItem(WorkItem.New("cr1", "/scheduled"));
            }
            using (_mocks.Playback())
            {
                var request = CreateCsvRequest("post", "/scheduled", "id=cr1");
                Assert.AreEqual(HttpStatusCode.Created, _httpHandler.HandleRequest(request).HttpStatusCode);
            }
        }

        [TestMethod]
        public void ShouldCreateWorkStepWhenPostingNewStep()
        {
            using (_mocks.Record())
            {
                _workflow.CreateWorkStep(new WorkStep("/analysis/inprocess", "/analysis", 0, WorkStepType.Normal, "cr"));
            }
            using (_mocks.Playback())
            {
                var request = CreateCsvRequest("post", "/analysis", "step=inprocess,class=cr");
                Assert.AreEqual(HttpStatusCode.Created, _httpHandler.HandleRequest(request).HttpStatusCode);
            }
        }

        [TestMethod]
        public void ShouldReturnHttpStatusCode405ForUnknownHttpMethod()
        {
            using (_mocks.Record())
            {
            }
            using (_mocks.Playback())
            {
                var request = CreateRequest("wrong", "/analysis", null, "text/html");
                Assert.AreEqual(HttpStatusCode.MethodNotAllowed, _httpHandler.HandleRequest(request).HttpStatusCode);
            }
        }

        [TestMethod]
        public void ShouldReturnHttpStatusCode415ForUnknownContentTypeForPost()
        {
            using (_mocks.Record())
            {
            }
            using (_mocks.Playback())
            {
                var request = CreateRequest("post", "/analysis", null, "text/wrong");
                Assert.AreEqual(HttpStatusCode.UnsupportedMediaType, _httpHandler.HandleRequest(request).HttpStatusCode);
            }
        }

        [TestMethod]
        public void ShouldMoveWorkItemWhenPostingExistingWorkItemToDifferentPath()
        {
            using (_mocks.Record())
            {
                Expect.Call(_workflow.ExistsWorkItem("cr1")).Repeat.AtLeastOnce().Return(true);
                _workflow.UpdateWorkItem(WorkItem.New("cr1", "/analysis"));
                LastCall.Repeat.Once();

            }
            using (_mocks.Playback())
            {
                var request = CreateCsvRequest("post", "/analysis", "id=cr1");
                Assert.AreEqual(HttpStatusCode.OK, _httpHandler.HandleRequest(request).HttpStatusCode);
            }
        }

        [TestMethod]
        public void ShouldRequestHtmlRendererWhenUsingGetWithHtmlContentType()
        {
            var renderer = _mocks.Stub<IWorkStepRenderer>();

            using (_mocks.Record())
            {
                Expect.Call(_rendererFactory.CreateRenderer("text/html")).Return(renderer);
            }
            using (_mocks.Playback())
            {
                var request = CreateHtmlRequest("get", "/", null);
                Assert.AreEqual(HttpStatusCode.OK, _httpHandler.HandleRequest(request).HttpStatusCode);
            }
        }

        [TestMethod]
        public void ShouldDeleteWorkItemWhenItemExistsInTheRightPath()
        {
            using (_mocks.Record())
            {
                Expect.Call(_workflow.ExistsWorkItem("cr1")).Return(true);
                Expect.Call(_workflow.GetWorkItem("cr1")).Return(WorkItem.New("cr1","/scheduled"));
                _workflow.DeleteWorkItem("cr1");
            }
            using (_mocks.Playback())
            {
                var request = CreateCsvRequest("delete", "/scheduled/cr1",null);
                Assert.AreEqual(HttpStatusCode.OK, _httpHandler.HandleRequest(request).HttpStatusCode);
            }
            
        }

        [TestMethod]
        public void ShouldReturnNotFoundWhenAttemptingToDeleteExistingWorkItemInTheWrongLocation()
        {
            using (_mocks.Record())
            {
                Expect.Call(_workflow.ExistsWorkItem("cr1")).Return(true);
                Expect.Call(_workflow.GetWorkItem("cr1")).Return(WorkItem.New("cr1", "/analysis"));
            }
            using (_mocks.Playback())
            {
                var request = CreateCsvRequest("delete", "/scheduled/cr1", null);
                Assert.AreEqual(HttpStatusCode.NotFound, _httpHandler.HandleRequest(request).HttpStatusCode);
            }

        }

        [TestMethod]
        public void ShouldReturnNotFoundWhenAttemptingToDeleteNonExistingWorkItem()
        {
            using (_mocks.Record())
            {
                Expect.Call(_workflow.ExistsWorkItem("cr1")).Return(false);
            }
            using (_mocks.Playback())
            {
                var request = CreateCsvRequest("delete", "/scheduled/cr1", null);
                Assert.AreEqual(HttpStatusCode.NotFound, _httpHandler.HandleRequest(request).HttpStatusCode);
            }

        }

        [TestMethod]
        public void ShouldReturnNotFoundWhenAttemptingToDeleteNonExistingWorkStep()
        {
            using (_mocks.Record())
            {
                Expect.Call(_workflow.ExistsWorkStep("/scheduled")).Return(false);
            }
            using (_mocks.Playback())
            {
                var request = CreateCsvRequest("delete", "/scheduled", null);
                Assert.AreEqual(HttpStatusCode.NotFound, _httpHandler.HandleRequest(request).HttpStatusCode);
            }

        }

        [TestMethod]
        public void ShouldReturnBadRequestWhenCausingArgumentExceptionInCreateWorkStep()
        {
            var exception = new ArgumentException();
            const HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;

            AssertExceptionIsCaughtAndHttpStatusCodeReturnedForCreateWorkStep(exception, httpStatusCode);
        }

        [TestMethod]
        public void ShouldReturnForbiddenWhenCausingInvalidOperationExceptionInCreateWorkStep()
        {
            var exception = new InvalidOperationException();
            const HttpStatusCode httpStatusCode = HttpStatusCode.Forbidden;

            AssertExceptionIsCaughtAndHttpStatusCodeReturnedForCreateWorkStep(exception, httpStatusCode);
        }


        [TestMethod]
        public void ShouldReturnBadRequestWhenCausingArgumentExceptionInCreateWorkItem()
        {
            var exception = new ArgumentException();
            const HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest;

            AssertExceptionIsCaughtAndHttpStatusCodeReturnedForCreateWorkItem(exception, httpStatusCode);
        }

        [TestMethod]
        public void ShouldReturnForbiddenWhenCausingInvalidOperationExceptionInCreateWorkItem()
        {
            var exception = new InvalidOperationException();
            const HttpStatusCode httpStatusCode = HttpStatusCode.Forbidden;

            AssertExceptionIsCaughtAndHttpStatusCodeReturnedForCreateWorkItem(exception, httpStatusCode);
        }

        private void AssertExceptionIsCaughtAndHttpStatusCodeReturnedForCreateWorkStep(Exception exception, HttpStatusCode httpStatusCode)
        {
            using (_mocks.Record())
            {
                _workflow.CreateWorkStep(new WorkStep("/scheduled", "/", 0, WorkStepType.Normal, "cr", null));
                LastCall.Throw(exception);
            }
            using (_mocks.Playback())
            {
                var request = CreateCsvRequest("post", "/", "step=/scheduled,class=cr");
                Assert.AreEqual(httpStatusCode, _httpHandler.HandleRequest(request).HttpStatusCode);
            }
        }

        private void AssertExceptionIsCaughtAndHttpStatusCodeReturnedForCreateWorkItem(Exception exception, HttpStatusCode httpStatusCode)
        {
            using (_mocks.Record())
            {
                _workflow.CreateWorkItem(WorkItem.New("id1", "/"));
                LastCall.Throw(exception);
            }
            using (_mocks.Playback())
            {
                var request = CreateCsvRequest("post", "/", "id=id1");
                Assert.AreEqual(httpStatusCode, _httpHandler.HandleRequest(request).HttpStatusCode);
            }
        }


        private static WorkflowHttpRequest CreateHtmlRequest(string httpMethod, string url, string httpMessage)
        {
            return CreateRequest(httpMethod, url, httpMessage, "text/html");
        }


        private static WorkflowHttpRequest CreateCsvRequest(string httpMethod, string url, string httpMessage)
        {
            return CreateRequest(httpMethod, url, httpMessage, "text/csv");
        }

        private static WorkflowHttpRequest CreateRequest(string httpMethod, string url, string httpMessage, string contentType)
        {
            var request =
                new WorkflowHttpRequest
                    {
                        ContentType = contentType,
                        HttpMethod = httpMethod,
                        RawUrl = url,
                        InputStream =
                            httpMessage != null
                                ? new MemoryStream(Encoding.ASCII.GetBytes(httpMessage))
                                : new MemoryStream()
                    };

            return request;
        }

    }
}