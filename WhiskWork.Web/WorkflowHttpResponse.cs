using System;
using System.IO;
using System.Net;
using WhiskWork.IO;

namespace WhiskWork.Web
{
    public class WorkflowHttpResponse
    {
        private readonly MemoryStream _outputStream = new MemoryStream();

        private WorkflowHttpResponse(HttpStatusCode statusCode)
        {
            HttpStatusCode = statusCode;
            Headers = new WebHeaderCollection();
        }

        public static WorkflowHttpResponse Ok
        {
            get { return new WorkflowHttpResponse(HttpStatusCode.OK); }
        }

        public static WorkflowHttpResponse UnsupportedMediaType
        {
            get { return new WorkflowHttpResponse(HttpStatusCode.UnsupportedMediaType); }
        }

        public static WorkflowHttpResponse NotFound
        {
            get { return new WorkflowHttpResponse(HttpStatusCode.NotFound); }
        }

        public static WorkflowHttpResponse BadRequest(Exception e)
        {
            var response = new WorkflowHttpResponse(HttpStatusCode.BadRequest);
            response.Write(e.ToString());
            return response;
        }

        public static WorkflowHttpResponse MethodNotAllowed
        {
            get { return new WorkflowHttpResponse(HttpStatusCode.MethodNotAllowed); }
        }

        public static WorkflowHttpResponse Forbidden(Exception e)
        {
            var response = new WorkflowHttpResponse(HttpStatusCode.Forbidden);
            response.Write(e.ToString());
            return response;
        }

        public static WorkflowHttpResponse NotImplemented
        {
            get { return new WorkflowHttpResponse(HttpStatusCode.NotImplemented); }
        }

        public static WorkflowHttpResponse InternalServerError(Exception e)
        {
            var response = new WorkflowHttpResponse(HttpStatusCode.InternalServerError);
            response.Write(e.ToString());
            return response;
        }

        public HttpStatusCode HttpStatusCode { get; private set; }

        public WebHeaderCollection Headers { get; private set; }

        public Stream OutputStream
        {
            get { return _outputStream; }
        }


        public static WorkflowHttpResponse Created(string locationUrl)
        {
            var response = new WorkflowHttpResponse(HttpStatusCode.Created);
            response.Headers.Add(HttpResponseHeader.Location, locationUrl);
            return response;
        }

        public void CopyOutputStreamTo(Stream toSteam)
        {
            var fromStream = new MemoryStream(_outputStream.ToArray());
            StreamUtil.CopyStream(fromStream, toSteam);
        }

        private void Write(string message)
        {
            using(var writer = new StreamWriter(OutputStream))
            {
                writer.Write(message);
            }
        }
    }
}