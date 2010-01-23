using System;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using WhiskWork.Core;
namespace WhiskWork.Web.UnitTest
{
    [TestClass]
    public class CsvRequestMessageParserTest
    {
        [TestMethod]
        public void ShouldParseIdAsWorkItem()
        {
            var parser = new CsvRequestMessageParser();
            var node = parser.Parse(CreateStream("id=id1")) as WorkItemNode;

            Assert.IsNotNull(node);
            var item = node.GetWorkItem("/");
            Assert.AreEqual("id1", item.Id);
        }

        [TestMethod]
        public void ShouldParseWorkItemProperties()
        {
            var parser = new CsvRequestMessageParser();
            var node = (WorkItemNode)parser.Parse(CreateStream("id=id1,name=name1,dev=dev1"));

            WorkItem item = node.GetWorkItem("/"); 
            Assert.AreEqual(2, item.Properties.Count);
            Assert.AreEqual("name1", item.Properties["name"]);
            Assert.AreEqual("dev1", item.Properties["dev"]);
        }

        [TestMethod]
        public void ShouldParseWorkItemOrdinal()
        {
            var parser = new CsvRequestMessageParser();
            var node = (WorkItemNode)parser.Parse(CreateStream("id=id1,ordinal=2"));

            var item = node.GetWorkItem("/");
            Assert.AreEqual(2,item.Ordinal);
        }

        [TestMethod]
        public void ShoudParseTimestamp()
        {
            var expectedTime = DateTime.Now;
            var xmlExpectedTime = XmlConvert.ToString(expectedTime, XmlDateTimeSerializationMode.RoundtripKind);

            var parser = new CsvRequestMessageParser();
            var node = (WorkItemNode)parser.Parse(CreateStream("id=id1,timestamp="+xmlExpectedTime));

            var item = node.GetWorkItem("/");
            Assert.AreEqual(expectedTime, item.Timestamp);

        }
        
        [TestMethod]
        public void ShouldParseStepAsWorkStep()
        {
            var parser = new CsvRequestMessageParser();
            var node = parser.Parse(CreateStream("step=step1,class=cr")) as WorkStepNode;

            Assert.IsNotNull(node);
            var step = node.GetWorkStep("/");
            Assert.AreEqual("/step1", step.Path);
        }

        [TestMethod]
        public void ShouldParseWorkStepProperties()
        {
            var parser = new CsvRequestMessageParser();
            var node = parser.Parse(CreateStream("step=step1,ordinal=1,title=title1,type=begin,class=class1")) as WorkStepNode;

            Assert.IsNotNull(node);
            var step = node.GetWorkStep("/");
            Assert.AreEqual("/step1", step.Path);
            Assert.AreEqual("/", step.ParentPath);
            Assert.AreEqual(1, step.Ordinal);
            Assert.AreEqual("title1", step.Title);
            Assert.AreEqual(WorkStepType.Begin, step.Type);
            Assert.AreEqual("class1", step.WorkItemClass);
        }


        private static Stream CreateStream(string value)
        {
            return new MemoryStream(Encoding.ASCII.GetBytes(value));
        }
    }
}
