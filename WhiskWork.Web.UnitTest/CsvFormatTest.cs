using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhiskWork.Test.Common;

namespace WhiskWork.Web.UnitTest
{
    [TestClass]
    public class CsvFormatTest
    {
        [TestMethod]
        public void ShouldParseUnquotedItems()
        {
            var actual = CsvFormat.Parse("one,two,three");
            Assert.AreEqual(3, actual.Count());
            Assert.AreEqual("one", actual.ElementAt(0));
            Assert.AreEqual("two", actual.ElementAt(1));
            Assert.AreEqual("three", actual.ElementAt(2));
        }

        [TestMethod]
        public void ShouldParseQuotedItems()
        {
            var actual = CsvFormat.Parse("\"one\",\"two\",\"three\"");
            Assert.AreEqual(3, actual.Count());
            Assert.AreEqual("one", actual.ElementAt(0));
            Assert.AreEqual("two", actual.ElementAt(1));
            Assert.AreEqual("three", actual.ElementAt(2));
        }

        [TestMethod]
        public void ShouldParseQuotedUnquotedAndQuotedItem()
        {
            var actual = CsvFormat.Parse("\"one\",two,\"three\"");
            Assert.AreEqual(3, actual.Count());
            Assert.AreEqual("one", actual.ElementAt(0));
            Assert.AreEqual("two", actual.ElementAt(1));
            Assert.AreEqual("three", actual.ElementAt(2));
        }

        [TestMethod]
        public void ShouldParseUnquotedQuotedAndUnquotedItem()
        {
            var actual = CsvFormat.Parse("one,\"two\",three");
            Assert.AreEqual(3, actual.Count());
            Assert.AreEqual("one", actual.ElementAt(0));
            Assert.AreEqual("two", actual.ElementAt(1));
            Assert.AreEqual("three", actual.ElementAt(2));
        }

        [TestMethod]
        public void ShouldParseQuotedItemWithComma()
        {
            var actual = CsvFormat.Parse("one,\"t,wo\",three");
            Assert.AreEqual(3, actual.Count());
            Assert.AreEqual("one", actual.ElementAt(0));
            Assert.AreEqual("t,wo", actual.ElementAt(1));
            Assert.AreEqual("three", actual.ElementAt(2));
        }


        [TestMethod]
        public void ShoulParseQuotedItemWithQuote()
        {
            var actual = CsvFormat.Parse("one,\"t\"wo\",three");
            Assert.AreEqual(3, actual.Count());
            Assert.AreEqual("one", actual.ElementAt(0));
            Assert.AreEqual("t\"wo", actual.ElementAt(1));
            Assert.AreEqual("three", actual.ElementAt(2));
        }

        [TestMethod]
        public void ShouldThrowIfMissingEndQuote()
        {
            AssertUtils.AssertThrows<ArgumentException>(
                () => CsvFormat.Parse("one,two,\"three").Count()
                );
        }

        [TestMethod]
        public void ShouldEscapeComma()
        {
            var actual = CsvFormat.Escape("a,comma");
            Assert.AreEqual("\"a,comma\"",actual);
        }
    }
}
