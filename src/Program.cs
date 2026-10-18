using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
namespace SecondHonk
{
    static class Program
    {
        internal static string ArtifactsFolder
        {
            get
            {
                return Environment.GetEnvironmentVariable("GOOSE_REPORT_DIR") ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".work", "reports");
            }
        }
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) => { Log(e.Exception); Application.Exit(); };
            try
            {
                Run(args);
            }
            catch (Exception error) { Log(error); Environment.ExitCode = 1; MessageBox.Show("The goose could not start. Check that your content folder is available and try again.", "Goose on the Loose", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        static void Run(string[] args)
        {
            string command = args.Length > 0 ? args[0].TrimStart('-') : "";
            if (command == "snapshot")
            {
                Snapshot(args);
                return;
            }
            if (command.Length > 0 && command != "seed-content" && CommandRegistry.Find(command) == null)
            {
                Environment.ExitCode = 2;
                return;
            }
            bool created;
            using (var mutex = new Mutex(true, CommandRegistry.Prefix + "Instance", out created))
            {
                if (!created)
                {
                    if (command.Length == 0)
                        command = "come";
                    if (command == "seed-content")
                        return;
                    for (int attempt = 0; attempt < 40; attempt++)
                        try
                        {
                            using (var signal = EventWaitHandle.OpenExisting(CommandRegistry.Prefix + command))
                                signal.Set();
                            if (command == "quit")
                            {
                                bool acquired = false;
                                try
                                {
                                    acquired = mutex.WaitOne(5000);
                                }
                                catch (AbandonedMutexException) { acquired = true; }
                                if (acquired)
                                    mutex.ReleaseMutex();
                                else
                                    Environment.ExitCode = 2;
                            }
                            return;
                        }
                        catch (WaitHandleCannotBeOpenedException) { Thread.Sleep(50); }
                    Environment.ExitCode = 2;
                    return;
                }
                if (command == "quit")
                    return;
                UserContent.Initialize(AppDomain.CurrentDomain.BaseDirectory);
                if (command == "seed-content")
                    return;
                if (command == "diagnostics")
                {
                    File.WriteAllText(Path.Combine(Store.Root, "diagnostics.txt"), Diagnostics.Summary());
                    return;
                }
                using (var session = new DesktopSession(Store.Load(), new Content(Store.Root), command))
                    Application.Run(session);
            }
        }
        static void Snapshot(string[] args)
        {
            string target = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(ArtifactsFolder, "goose.png");
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            var world = new World(new SaveData(), new Content(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Packs")), 42);
            world.Goose.Place(new V(130, 145));
            world.Goose.Direction = world.Goose.HeadDirection = -.3f;
            world.Update(0);
            using (var bitmap = new System.Drawing.Bitmap(280, 210))
            {
                using (var drawing = System.Drawing.Graphics.FromImage(bitmap))
                {
                    drawing.Clear(System.Drawing.Color.Transparent);
                    Art.Scene(drawing, world);
                }
                bitmap.Save(target, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        internal static void Log(Exception error)
        {
            Diagnostics.Log(error);
        }
    }
}
