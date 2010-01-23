using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WhiskWork.Test.Common
{
    public static class AssertUtils
    {
        public static void AssertThrows<T>(Action action) where T : Exception
        {
            try
            {
                action();
                Assert.Fail("Expected exception of type {0}", typeof (T));
            }
            catch (T)
            {
            }
        }

    }
}