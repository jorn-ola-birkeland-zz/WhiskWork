using System.IO;
using System.Linq;
using System.Net;

namespace WhiskWork.Web
{
    public class WorkflowHttpRequest
    {
        public string Accept  { get; set; }
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
                        ContentType = listenerRequest.ContentType,
                        Accept = listenerRequest.AcceptTypes != null ? listenerRequest.AcceptTypes.FirstOrDefault() : null
                    };

            return request;
        }
    }
}