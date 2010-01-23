using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

        public static string GetParentPath(string path)
        {
            ThrowIfInvalidPath(path, "path");

            var pathParts = path.Split(Separator);

            var result = pathParts.Where((s, index) => index != pathParts.Length - 1).Aggregate(CombinePath);

            if (String.IsNullOrEmpty(result))
            {
                result = new string(new[] { Separator });
            }

            return result;
        }


        public static bool IsValidPath(string path)
        {
            var regex = new Regex(@"^(\/)$|^(\/[0-9,a-z,A-Z,\-]+)+$");

            return regex.IsMatch(path);
        }

        private static void ThrowIfInvalidPath(string path, string paramName)
        {
            if (!IsValidPath(path))
            {
                throw new ArgumentException("Invalid path", paramName);
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

        public static string GetLeafDirectory(string path)
        {
            var leafDirectory = path.Split(Separator).LastOrDefault();

            if(string.IsNullOrEmpty(leafDirectory))
            {
                leafDirectory = null;
            }

            return leafDirectory;
        }
    }
}