using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using Abb.One.MicroWebServer;
using WhiskWork.Core;
using WhiskWork.IO;
using WhiskWork.Web;

namespace WhiskWork.CommondLineWebServer
{
    internal class WebRouter
    {
        private readonly string _rootFileDirectory;
        private readonly WorkflowHttpHandler _workflowHandler;

        public WebRouter(string webDirectory)
        {
            _rootFileDirectory = webDirectory;

            const string logfile = @"c:\temp\agileboard\workflow.log";

            var memoryWorkStepRepository = new MemoryWorkflowRepository();

            var workStepRepository = memoryWorkStepRepository;
            var logger = new FileWorkItemLogger(logfile);
            var workItemRepository = new LoggingWorkItemRepository(logger, new MemoryWorkItemRepository());
            var wp = new Workflow(workStepRepository, workItemRepository);

            var rendererFactory = new HtmlWorkStepRendererFactory(workItemRepository,workStepRepository);
            _workflowHandler = new WorkflowHttpHandler(wp, rendererFactory);
        }


        public void ProcessRequest(IHttpListenerContext httpcontext)
        {
            if (TryReturnFile(httpcontext.Response, httpcontext.Request.RawUrl))
            {
                return;
            }

            ProcessWorkflowRequest(httpcontext);
        }

        private bool TryReturnFile(HttpListenerResponse response, string path)
        {
            string filePath = path.Remove(0, 1).Replace('/', '\\');
            string fullPath = Path.Combine(_rootFileDirectory, filePath);

            if (!File.Exists(fullPath))
            {
                return false;
            }

            response.ContentType = GetContentType(fullPath);

            using (Stream instream = File.OpenRead(fullPath))
            {
                StreamUtil.CopyStream(instream, response.OutputStream);
            }

            return true;
        }

        private void ProcessWorkflowRequest(IHttpListenerContext httpcontext)
        {
            var request = WorkflowHttpRequest.Create(httpcontext.Request);

            var response = _workflowHandler.HandleRequest(request);

            CopyResponse(response, httpcontext.Response);
        }


        private static void CopyResponse(WorkflowHttpResponse fromResponse, HttpListenerResponse response)
        {
            response.StatusCode = (int) fromResponse.HttpStatusCode;
            CopyHeaders(fromResponse.Headers, response.Headers);
            fromResponse.CopyOutputStreamTo(response.OutputStream);
        }

        private static string GetContentType(string path)
        {
            var fi = new FileInfo(path);

            switch (fi.Extension)
            {
                case ".css":
                    return "text/css";
                case ".html":
                    return "text/html";
                case ".js":
                    return "application/javascript";
            }

            return "text/plain";
        }


        private static void CopyHeaders(NameValueCollection fromHeaders, NameValueCollection toHeaders)
        {
            foreach (string key in fromHeaders.AllKeys)
            {
                toHeaders[key] = fromHeaders[key];
            }
        }
    }
}