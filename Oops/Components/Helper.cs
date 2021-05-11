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
            /*
            Console.WriteLine("Directory.GetCurrentDirectory:" + System.IO.Directory.GetCurrentDirectory());
            Console.WriteLine($"Launched from {Environment.CurrentDirectory}");
            Console.WriteLine($"Physical location {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"AppContext.BaseDir {AppContext.BaseDirectory}");            
            Console.WriteLine($"Runtime Call {Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)}" + Path.DirectorySeparatorChar);
            */
        }

    }
}