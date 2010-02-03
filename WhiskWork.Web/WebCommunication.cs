using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using System.Xml;
using WhiskWork.Core;

namespace WhiskWork.Web
{
    public interface IHttpRequest
    {
        string Accept { get; set; }
        string Method { get; set; }
        string ContentType { get; set; }
        long ContentLength { get; set; }
        Stream GetRequestStream();
        IHttpResponse GetResponse();
    }

    public interface IHttpResponse : IDisposable
    {
        Stream GetResponseStream();
    }

    public interface IHttpRequestFactory
    {
        IHttpRequest Create(string url);
    }

    internal class HttpRequestFactory : IHttpRequestFactory
    {
        public IHttpRequest Create(string url)
        {
            return new HttpRequest((HttpWebRequest)WebRequest.Create(url));
        }
    }

    internal class HttpRequest : IHttpRequest
    {
        private readonly HttpWebRequest _request;

        public HttpRequest(HttpWebRequest webRequest)
        {
            _request = webRequest;
        }

        public string Accept
        {
            get { return _request.Accept; }
            set { _request.Accept = value; }
        }

        public string Method
        {
            get { return _request.Method; }
            set { _request.Method = value; }
        }

        public string ContentType
        {
            get { return _request.ContentType; }
            set { _request.ContentType = value; }
        }

        public long ContentLength
        {
            get { return _request.ContentLength; }
            set { _request.ContentLength = value; }
        }

        public Stream GetRequestStream()
        {
            return _request.GetRequestStream();
        }

        public IHttpResponse GetResponse()
        {
            return new HttpResponse(_request.GetResponse());
        }
    }

    internal class HttpResponse : IHttpResponse
    {
        private readonly WebResponse _response;
        public HttpResponse(WebResponse response)
        {
            _response = response;
        }

        public void Dispose()
        {
            ((IDisposable)_response).Dispose();
        }

        public Stream GetResponseStream()
        {
            return _response.GetResponseStream();
        }
    }

    public class WebCommunication
    {
        private readonly IHttpRequestFactory _httpRequestFactory;
        public WebCommunication() : this(new HttpRequestFactory())
        {
            
        }

        public WebCommunication(IHttpRequestFactory httpRequestFactory)
        {
            _httpRequestFactory = httpRequestFactory;
        }

        public XmlDocument GetXmlDocument(string url)
        {
            var request = _httpRequestFactory.Create(url);

            request.Accept = "text/xml";
            request.Method = "GET";

            var doc = new XmlDocument();

            using(var response = request.GetResponse())
            {
                doc.Load(response.GetResponseStream());
            }

            return doc;
        }

        public void SendCsvRequest(string url, string httpverb, string payload)
        {
            var request = _httpRequestFactory.Create(url);

            request.ContentType = "text/csv";
            request.Method = httpverb;
            request.ContentLength = payload.Length;

            using (var writer = new StreamWriter(request.GetRequestStream()))
            {
                writer.Write(payload);
            }

            var response = (IDisposable)request.GetResponse();
            response.Dispose();
        }

        public void PostCsv(string url, IDictionary<string,string> keyValues)
        {
            var payload = CreatePayload(keyValues);

            SendCsvRequest(url,"post",payload);
        }

        public void PostCsv(string host, WorkItem workItem)
        {
            var url = host + workItem.Path;

            var keyValues = new Dictionary<string, string> {{"id", workItem.Id}};

            if(workItem.Ordinal.HasValue)
            {
                keyValues.Add("ordinal",workItem.Ordinal.Value.ToString());
            }
            if (workItem.Timestamp.HasValue)
            {
                keyValues.Add("timestamp", XmlConvert.ToString(workItem.Timestamp.Value,XmlDateTimeSerializationMode.RoundtripKind));
            }

            foreach (var keyValuePair in workItem.Properties)
            {
                keyValues.Add(keyValuePair.Key,keyValuePair.Value);
            }

            var payload = CreatePayload(keyValues);

            SendCsvRequest(url, "post", payload);
        }


        private static string CreatePayload(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            var payloadBuilder = new StringBuilder();
            var first = true;


            foreach (var keyValuePair in keyValues)
            {
                if(!first)
                {
                    payloadBuilder.Append(",");
                }

                var item = string.Format("{0}={1}", HttpUtility.HtmlEncode(keyValuePair.Key),
                                         HttpUtility.HtmlEncode(keyValuePair.Value));
                payloadBuilder.Append(CsvFormat.Escape(item));

                first = false;
            }

            return payloadBuilder.ToString();
        }

        public static string ReadResponseToEnd(WebResponse response)
        {
            string result;
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }
    }
}