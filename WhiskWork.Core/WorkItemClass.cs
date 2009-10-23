using System.Collections.Generic;

namespace WhiskWork.Core
{
    public static class WorkItemClass
    {
        private const char _separator = '-';

        public static IEnumerable<string> FindRootClasses(string workItemClass)
        {
            string[] classParts = workItemClass.Split(_separator);
            if(classParts.Length>1)
            {
                yield return classParts[0];
            }
        }

        public static string Combine(params string[] classParts)
        {
            return string.Join(new string(_separator,1), classParts);
        }
    }
}