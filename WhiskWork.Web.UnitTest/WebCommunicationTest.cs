using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using WhiskWork.Core;
namespace WhiskWork.Web.UnitTest
{
    [TestClass]
    public class WebCommunicationTest
    {


        [TestMethod]
        public void ShouldQuoteAndHtmlEncodeCsvItemsContainingComma()
        {
            var mocks = new MockRepository();

            var httpRequestFactoryStub = mocks.Stub<IHttpRequestFactory>();
            var requestStub = mocks.Stub<IHttpRequest>();
            var responseStub = mocks.Stub<IHttpResponse>();

            var stream = new MemoryStream();

            using(mocks.Record())
            {
                SetupResult.For(httpRequestFactoryStub.Create(null)).IgnoreArguments().Return(requestStub);
                SetupResult.For(requestStub.GetRequestStream()).Return(stream);
                SetupResult.For(requestStub.GetResponse()).Return(responseStub);
            }
            
            var comm = new WebCommunication(httpRequestFactoryStub);

            comm.PostCsv("http://test.com",WorkItem.New("1","/").UpdateProperty("title","title,with,comma"));

            using(var reader = new StreamReader(new MemoryStream(stream.ToArray())))
            {
                var actual = reader.ReadToEnd();

                Assert.AreEqual("id=1,\"title=title,with,comma\"",actual);
            }
        }
    }
}
