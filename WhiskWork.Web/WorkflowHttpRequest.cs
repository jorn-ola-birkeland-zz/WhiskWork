using System.IO;
using System.Net;

namespace WhiskWork.Web
{
    public class WorkflowHttpRequest
    {
        public string ContentType { get; private set; }

        public string HttpMethod { get; private set; }

        public string RawUrl { get; private set; }

        public Stream InputStream { get; private set; }

        public static WorkflowHttpRequest Create(HttpListenerRequest listenerRequest)
        {
            var request =
                new WorkflowHttpRequest
                    {
                        InputStream = listenerRequest.InputStream,
                        RawUrl = listenerRequest.RawUrl,
                        HttpMethod = listenerRequest.HttpMethod,
                        ContentType = listenerRequest.ContentType
                    };

            return request;
        }
    }
}