using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SecondHonk
{
    static class Diagnostics
    {
        static readonly object sync = new object();
        public static void Log(Exception error)
        {
            try
            {
                lock (sync)
                {
                    Directory.CreateDirectory(Store.Root);
                    string path = Path.Combine(Store.Root, "error.log");
                    if (File.Exists(path) && new FileInfo(path).Length >= 1048576)
                    {
                        if (File.Exists(path + ".2"))
                            File.Delete(path + ".2");
                        if (File.Exists(path + ".1"))
                            File.Move(path + ".1", path + ".2");
                        File.Move(path, path + ".1");
                    }

                    string message = error.ToString();
                    if (message.Length > 16384)
                        message = message.Substring(0, 16384);
                    File.AppendAllText(path, DateTime.UtcNow.ToString("s") + " " + message + Environment.NewLine);
                }
            }
            catch (Exception)
            {
            }
        }

        // Intentionally excludes paths, custom content and log text.
        public static string Summary()
        {
            using (var p = Process.GetCurrentProcess())
                return "Goose on the Loose " + typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion + Environment.NewLine + "Windows " + Environment.OSVersion.Version + "; CLR " + Environment.Version + Environment.NewLine + "Private memory: " + (p.PrivateMemorySize64 / 1048576) + " MB; handles: " + p.HandleCount + Environment.NewLine + "GC collections: " + GC.CollectionCount(0) + " / " + GC.CollectionCount(1) + " / " + GC.CollectionCount(2);
        }
    }
}
