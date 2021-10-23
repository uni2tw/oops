using System;
using System.Diagnostics;
using System.IO;

namespace Oops.Components
{

    public class Helper
    {
        private static string _rootPath;
        public static void Log(string message)
        {
            Console.WriteLine(message);
        }

        public static string MapPath(string relPath)
        {
            if (_rootPath == null)
            {
                _rootPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) +
                    Path.DirectorySeparatorChar;
            }
            string result = Path.Combine(_rootPath, relPath);
            return result;            
        }

    }
}