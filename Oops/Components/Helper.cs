using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Oops.Components
{

    public class Helper
    {
        private static string _rootPath;

        public static string MapPath(string relPath)
        {
            if (_rootPath == null)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    _rootPath = Directory.GetCurrentDirectory();
                    // Do something
                }
                else
                {
                    _rootPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) +
                        Path.DirectorySeparatorChar;
                }
            }
            string result = Path.Combine(_rootPath, relPath);
            return result;            
        }

        public static string GetRoot()
        {
            return _rootPath;
        }

        public static void SetRoot(string rootPath)
        {
            _rootPath = rootPath;
        }
    }
}