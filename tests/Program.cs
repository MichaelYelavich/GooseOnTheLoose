using System;
using System.IO;
using System.Windows.Forms;
namespace SecondHonk
{
    static class TestRunner
    {
        [STAThread]
        static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var content = new Content(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Packs"));
            if (args.Length > 0 && args[0] == "--benchmark")
            {
                Performance.Main(new[] { args.Length > 1 ? args[1] : Path.Combine(Program.ArtifactsFolder, "performance.txt"), args.Length > 2 ? args[2] : "0" });
                return Environment.ExitCode;
            }
            if (args.Length > 0 && args[0] == "--native-smoke")
                SelfTest.NativeSmoke(content);
            else
                SelfTest.Run(content);
            return Environment.ExitCode;
        }
    }
}
