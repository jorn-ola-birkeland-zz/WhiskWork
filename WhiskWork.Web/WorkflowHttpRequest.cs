using System.IO;
using System.Net;

namespace WhiskWork.Web
{
    public class WorkflowHttpRequest
    {
        public string ContentType { get; set; }

        public string HttpMethod { get; set; }

        public string RawUrl { get; set; }

        public Stream InputStream { get; set; }

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