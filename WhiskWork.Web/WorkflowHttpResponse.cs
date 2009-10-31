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

        public static WorkflowHttpResponse BadRequest
        {
            get { return new WorkflowHttpResponse(HttpStatusCode.BadRequest); }
        }

        public static WorkflowHttpResponse MethodNotAllowed
        {
            get { return new WorkflowHttpResponse(HttpStatusCode.MethodNotAllowed); }
        }

        public static WorkflowHttpResponse Forbidden
        {
            get { return new WorkflowHttpResponse(HttpStatusCode.Forbidden); }
        }

        public static WorkflowHttpResponse InternalServerError
        {
            get { return new WorkflowHttpResponse(HttpStatusCode.InternalServerError); }
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

        public static WorkflowHttpResponse MovedPermanently(string locationUrl)
        {
            var response = new WorkflowHttpResponse(HttpStatusCode.MovedPermanently);
            response.Headers.Add(HttpResponseHeader.Location, locationUrl);
            return response;
        }

        public void CopyOutputStreamTo(Stream toSteam)
        {
            var fromStream = new MemoryStream(_outputStream.ToArray());
            StreamUtil.CopyStream(fromStream, toSteam);
        }
    }
}