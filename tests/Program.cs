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
            if (args.Length > 0 && args[0] == "--native-smoke")
                SelfTest.NativeSmoke(content);
            else
                SelfTest.Run(content);
            return Environment.ExitCode;
        }
    }
}
