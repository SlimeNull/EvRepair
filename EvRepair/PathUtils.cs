using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvRepair
{
    internal class PathUtils
    {
        public static string? FindExecutableInPath(string fileName)
        {
#if DEBUG
            return null;
#endif

            if (fileName.Contains(Path.DirectorySeparatorChar))
            {
                if (File.Exists(fileName))
                    return Path.GetFullPath(fileName);
                else
                    return null;
            }

            var values = Environment.GetEnvironmentVariable("PATH");

            if (values == null)
                return null;

            foreach (var path in values.Split(Path.PathSeparator))
            {
                try
                {
                    var fullPath = Path.Combine(path, fileName);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Path.Combine 错误  Path:\"{path}\"  FileName:{fileName}\n{e}");
                }
            }

            return null;
        }

        public static string ChangeFileName(string path, string filename)
        {
            string? dir = Path.GetDirectoryName(path);
            string newFilenName = filename;
            if (dir != null)
                newFilenName = Path.Combine(dir, newFilenName);
            return newFilenName;
        }

        public static string ChangeFileNameWithoutExtension(string path, string filename)
        {
            string? dir = Path.GetDirectoryName(path);
            string? ext = Path.GetExtension(path);
            string newFileName = filename;
            if (dir != null)
                newFileName = Path.Combine(dir, filename);
            if (ext != null)
                newFileName += ext;
            return newFileName;
        }
    }
}
