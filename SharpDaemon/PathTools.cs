using System;
using System.IO;
using System.Collections.Generic;

namespace SharpDaemon
{
    public static class PathTools
    {
        //checks against existing subfolders
        public static bool HasDirectChild(string folder, string child)
        {
            var root = Path.GetFullPath(folder);
            var children = new List<string>(Directory.GetDirectories(root));
            return children.Contains(Combine(folder, child));
        }

        //path level check
        public static bool IsChildPath(string folder, string child)
        {
            var root = Path.GetFullPath(folder);
            var path = Combine(folder, child);
            //check there is something after root
            if (path.Length <= root.Length) return false;
            //check root is parent
            if (!path.Contains(root)) return false;
            //check child name contains no navigation leading somewhere else
            //length + 1 to remove path separator as well
            if (child != path.Substring(root.Length + 1)) return false;
            return true;
        }

        public static string Combine(string folder, string format, params object[] args)
        {
            var relative = TextTools.Format(format, args);
            var root = Path.GetFullPath(folder);
            return Path.GetFullPath(Path.Combine(root, relative));
        }
    }
}
