using System;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml;
using WhiskWork.Generic;

namespace WhiskWork.Web.UnitTest
{
    [TestClass]
    public class XmlParserTest
    {
        [TestMethod]
        public void ShouldParseMinimalWorkItem()
        {
            var xml = CreateSingleWorkItemXml("wi1","");
            var workItems = XmlParser.ParseWorkItems(CreateXmlDocument(xml));
            Assert.AreEqual("wi1",workItems.ElementAt(0).Id);
        }

        [TestMethod]
        public void ShouldParseWorkItemWithLastMoved()
        {
            var xml = CreateSingleWorkItemXml("wi1", "lastmoved=\"2009-12-02T14:51:23.233\"");


            var workItems = XmlParser.ParseWorkItems(CreateXmlDocument(xml));
            Assert.AreEqual(new DateTime(2009,12,2,14,51,23,233), workItems.ElementAt(0).LastMoved.Value);
        }

        [TestMethod]
        public void ShouldParseWorkItemWithClasses()
        {
            var xml = CreateSingleWorkItemXml("wi1", "classes=\"class1 class2 class3\"");

            var workItems = XmlParser.ParseWorkItems(CreateXmlDocument(xml));
            Assert.AreEqual("class1,class2,class3", workItems.ElementAt(0).Classes.Join(','));
        }

        private static string CreateSingleWorkItemXml(string id, string attributeValue)
        {
            var sb = new StringBuilder();

            sb.Append("<WorkSteps>");
            sb.Append("<WorkStep id=\"step\">");
            sb.Append("<WorkItems>");
            sb.AppendFormat("<WorkItem id=\"{0}\" {1}>", id,attributeValue);
            sb.Append("</WorkItem>");
            sb.Append("</WorkItems>");
            sb.Append("</WorkStep>");
            sb.Append("</WorkSteps>");

            return sb.ToString();
        }


        private static XmlDocument CreateXmlDocument(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            return doc;
        }
    }
}
