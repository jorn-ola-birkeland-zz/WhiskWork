using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace WhiskWork.Generic
{
    public static class StringExtensions
    {
        public static string Join(this IEnumerable<string> values, char separator)
        {
            if(values.Count()==0)
            {
                return string.Empty;
            }

            var sep = new string(new[] {separator });

            return values.Aggregate((current, next) => current + sep + next);
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