using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace WhiskWork.UnitTest
{
    internal static class StringExtensions
    {
        public static string Join(this IEnumerable<string> values, char separator)
        {
            var sb = new StringBuilder();
            var isFirst = true;
            foreach (var s in values)
            {
                if(!isFirst)
                {
                    sb.Append(separator);
                }
                sb.Append(s);

                isFirst = false;
            }

            return sb.ToString();
        }

        public static bool SetEquals<T>(this IEnumerable<T> values, params T[] set)
        {
            return SetEquals(values, (IEnumerable<T>)set);
        }


        public static bool SetEquals<T>(this IEnumerable<T> values, IEnumerable<T> set)
        {
            if(values.Count()!=set.Count())
            {
                return false;
            }

            return values.All(s => set.Contains(s));
        }
    }
}