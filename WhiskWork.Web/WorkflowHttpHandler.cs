using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using WhiskWork.Core;

namespace WhiskWork.Web
{
    public class WorkflowHttpHandler
    {
        private readonly IWorkItemRepository _workItemRepository;
        private readonly IWorkflowRepository _workStepRepository;
        private readonly Workflow _wp;

        public WorkflowHttpHandler()
        {
            const string logfile = @"c:\temp\agileboard\workflow.log";

            var memoryWorkStepRepository = new MemoryWorkflowRepository();

            _workStepRepository = memoryWorkStepRepository;
            var logger = new FileWorkItemLogger(logfile);
            _workItemRepository = new LoggingWorkItemRepository(logger, new MemoryWorkItemRepository());
            _wp = new Workflow(_workStepRepository, _workItemRepository);
        }

        public WorkflowHttpResponse HandleRequest(WorkflowHttpRequest request)
        {
            string path = request.RawUrl;
            string httpMethod = request.HttpMethod;

            string actualPath;
            string workItemId;
            
            switch (httpMethod.ToLowerInvariant())
            {
                case "post":
                    return RespondToPost(request);
                case "get":
                    return RenderHtml(path);
                case "delete":
                    if (IsWorkItem(path, out actualPath, out workItemId))
                    {
                        return DeleteWorkItem(actualPath, workItemId);
                    }

                    break;

                case "put":
                    //return RespondToPut(request);


                    if (IsWorkItem(path, out actualPath, out workItemId))
                    {
                        return UpdateWorkItem(actualPath, workItemId, null /*, payload*/);
                    }
                    break;
            }
            return WorkflowHttpResponse.MethodNotAllowed;
        }

        private WorkflowHttpResponse RespondToPut(WorkflowHttpRequest request)
        {
            IRequestMessageParser parser;
            if (!TryLocateParser(request.ContentType, out parser))
            {
                return WorkflowHttpResponse.UnsupportedMediaType;
            }

            var visitor = new HttpPutWorkflowNodeVisitor(_wp, request.RawUrl);
            parser.Parse(request.InputStream).AcceptVisitor(visitor);

            return visitor.Response;
        }

        private WorkflowHttpResponse RespondToPost(WorkflowHttpRequest request)
        {
            IRequestMessageParser parser;
            if (!TryLocateParser(request.ContentType, out parser))
            {
                return WorkflowHttpResponse.UnsupportedMediaType;
            }

            var visitor = new HttpPostWorkflowNodeVisitor(_wp, request.RawUrl);
            parser.Parse(request.InputStream).AcceptVisitor(visitor);

            return visitor.Response;
        }

        private static bool TryLocateParser(string contentType, out IRequestMessageParser parser)
        {
            return RequestMessageParserFactory.TryCreate(contentType, out parser);
        }

        private bool IsWorkItem(string rawPath, out string pathPart, out string workItemId)
        {
            pathPart = rawPath;
            workItemId = null;

            string potentialWorkItemId = rawPath.Split('/').Last();

            if (_wp.ExistsWorkItem(potentialWorkItemId))
            {
                workItemId = potentialWorkItemId;
                pathPart = rawPath.Substring(0, rawPath.LastIndexOf('/'));
                return true;
            }

            return false;
        }

        private WorkflowHttpResponse DeleteWorkItem(string path, string id)
        {
            if (!_wp.ExistsWorkItem(id))
            {
                return WorkflowHttpResponse.NotFound;
            }

            WorkItem wi = _wp.GetWorkItem(id);

            if (wi.Path != path)
            {
                return WorkflowHttpResponse.NotFound;
            }

            try
            {
                _wp.DeleteWorkItem(id);
            }
            catch (Exception e)
            {
                Console.WriteLine("Delete failed " + e.Message);
                return WorkflowHttpResponse.Forbidden;
            }

            return WorkflowHttpResponse.Ok;
        }

        private WorkflowHttpResponse UpdateWorkItem(string path, string id, string payload)
        {
            if (!_wp.ExistsWorkItem(id) || !_wp.ExistsWorkStep(path))
            {
                return WorkflowHttpResponse.NotFound;
            }

            WorkItem updatdWorkItem;
            try
            {
                updatdWorkItem = _wp.UpdateWorkItem(id, path, new NameValueCollection());
            }
            catch (Exception e)
            {
                Console.WriteLine("Update failed " + e.Message);
                return WorkflowHttpResponse.Forbidden;
            }

            //return updatdWorkItem.Path != path
            //           ? WorkflowHttpResponse.MovedPermanently(updatdWorkItem.Path + "/" + id)
            //           : WorkflowHttpResponse.Ok;

            return WorkflowHttpResponse.Ok;
        }

        private WorkflowHttpResponse RenderHtml(string path)
        {
            var renderer = new HtmlRenderer(_workStepRepository, _workItemRepository);
            try
            {
                WorkflowHttpResponse response = WorkflowHttpResponse.Ok;
                renderer.RenderFull(response.OutputStream, path);

                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine("Render failed " + e.Message);
                return WorkflowHttpResponse.InternalServerError;
            }
        }

    }

}