using System;
using System.Collections.Generic;
using System.Linq;

namespace WhiskWork.Core
{
    public static class WorkflowPath
    {
        public const char Separator = '/';
        private static readonly string _separator = new string(new[] { Separator });


        public static string CombinePath(string path1, string path2)
        {
            path1 = RemoveTrailingSeparator(path1);
            path2 = RemoveLeadingSeparator(path2);

            var result = path1 + _separator + path2;

            if(result.Length>1)
            {
                result = RemoveTrailingSeparator(result);
            }

            return result;
        }

        public static string FindCommonRoot(string path1, string path2)
        {
            var subPaths1 = GetSubPaths(path1);
            var subPaths2 = GetSubPaths(path2);

            return subPaths1.Intersect(subPaths2).Last();
        }

        public static IEnumerable<string> GetPathsBetween(string path1, string path2)
        {
            if (FindCommonRoot(path1, path2) != path1)
            {
                throw new ArgumentException("path1 must be subpath of path2");
            }

            var diffPath = path2.Remove(0, path1.Length);

            foreach (var subDiffPath in GetSubPaths(diffPath))
            {
                yield return CombinePath(path1, subDiffPath);
            }
        }

        public static IEnumerable<string> GetSubPaths(string path)
        {
            var current = Separator.ToString();

            foreach (var pathPart in path.Split(Separator))
            {
                current = CombinePath(current, pathPart);
                yield return current;
            }
        }


        private static string RemoveLeadingSeparator(string path2)
        {
            return path2.StartsWith(_separator) ? path2.Remove(0, 1) : path2;
        }

        private static string RemoveTrailingSeparator(string path)
        {
            return path.EndsWith(_separator) ? path.Remove(path.Length - 1, 1) : path;
        }
    }
}