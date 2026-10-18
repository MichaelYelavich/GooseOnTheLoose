using System.Linq;
using System;
using System.IO;
namespace SecondHonk
{
    static class BetaTests
    {
        public static void Run(Content content, Action<bool, string> check)
        {
            var local = new Content(Path.Combine(Store.Root, "empty"));
            local.Images.Add("vanishing.png");
            var world = new World(new SaveData { Sound = false, Mischief = false, Scale = 1 }, local, 42);
            world.Deliver(true);
            local.Images.Clear();
            local.Notes.Clear();
            for (int i = 0; i < 1200; i++)
                world.Update(1f / 60);
            check(world.Deliveries == 1 && world.DeliveryImage == "vanishing.png" && world.DeliveryText.Length > 0,
                "Delivery snapshots its payload before live content disappears");

            string documents = Path.Combine(Store.Root, "Documents probe");
            Directory.CreateDirectory(documents);
            check(ProfilePaths.Resolve(documents) == Path.Combine(documents, "GooseOnTheLoose"), "Fresh profiles use the new Documents folder");
            string legacy = Path.Combine(documents, "DesktopGoose2"), fresh = Path.Combine(documents, "GooseOnTheLoose");
            Directory.CreateDirectory(legacy);
            File.WriteAllText(Path.Combine(legacy, "settings.json"), "{}");
            check(ProfilePaths.Resolve(documents) == legacy, "Existing Documents content remains in its original folder");
            Directory.CreateDirectory(fresh);
            File.WriteAllText(Path.Combine(fresh, "settings.json"), "{}");
            check(ProfilePaths.Resolve(documents) == fresh, "An initialized new profile takes precedence without merging collections");
            string note = Path.Combine(Store.Root, "Notes", "Atomic.txt");
            File.WriteAllText(note, "original");
            bool conflict;
            string saved = AtomicNotes.Save(note, "original", "edited", out conflict);
            check(!conflict && saved == note && File.ReadAllText(note) == "edited", "Normal note saves atomically replace the original file");
            File.WriteAllText(note, "external edit");
            saved = AtomicNotes.Save(note, "edited", "my edit", out conflict);
            check(conflict && saved != note && File.ReadAllText(note) == "external edit" && File.ReadAllText(saved) == "my edit", "Concurrent note edits preserve both versions in a conflict copy");
            File.Delete(note);
            saved = AtomicNotes.Save(note, "edited", "unsaved", out conflict);
            check(conflict && !File.Exists(note) && File.ReadAllText(saved) == "unsaved", "Saving after external deletion preserves a copy without recreating the removed note");
            File.WriteAllText(note, "locked");
            bool blocked = false;
            using (var held = new FileStream(note, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                try
                {
                    AtomicNotes.Save(note, "locked", "overwrite", out conflict);
                }
                catch (IOException) { blocked = true; }
            check(blocked && File.ReadAllText(note) == "locked" && !Directory.GetFiles(Path.Combine(Store.Root, "Notes"), "*.tmp").Any(), "Locked-note failures preserve the file and leave no temporary files");
            using (var editor = new NoteEditor())
            {
                File.WriteAllText(note, "external change while editor open");
                editor.Close();
            }
            check(File.ReadAllText(note) == "external change while editor open", "Closing an unchanged note editor cannot overwrite an external edit");
            check(!ContentMonitor.IgnoredEvent(null) && ContentMonitor.IgnoredEvent("error.log.2") && ContentMonitor.IgnoredEvent("runtime.json.tmp"), "Watcher handles missing names and ignores diagnostic and temporary writes");
            var contentWorld = new World(new SaveData(), new Content(Store.Root), 42);
            using (var monitor = new ContentMonitor(contentWorld))
            {
                string notes = Path.Combine(Store.Root, "Notes"), away = notes + "-unavailable";
                int count = contentWorld.Content.Notes.Count;
                Directory.Move(notes, away);
                monitor.Reload();
                check(contentWorld.Content.Notes.Count == count, "Unavailable content folders retain the previous valid snapshot");
                Directory.Move(away, notes);
                File.WriteAllText(Path.Combine(notes, "Recovered.txt"), "Recovered folder");
                monitor.Reload();
                check(contentWorld.Content.Notes.Contains("Recovered folder"), "Content reload recovers when an unavailable folder returns");
            }
            string imports = Path.Combine(Store.Root, "legacy-count");
            Directory.CreateDirectory(imports);
            File.WriteAllText(Path.Combine(imports, "one.txt"), "A note");
            File.WriteAllText(Path.Combine(imports, "two.json"), "{\"notes\":[\"A\",\"B\"]}");
            var imported = UserContent.ImportLegacyFolder(imports);
            check(imported.Imported == 3 && imported.Errors.Count == 0, "Legacy import summaries count text files and individual JSON notes");
            using (var cancel = new System.Threading.CancellationTokenSource())
            {
                cancel.Cancel();
                check(UserContent.ImportLegacyFolder(imports, cancel.Token).Imported == 0, "Cancelled legacy imports do not publish notes");
            }
            string picture = Path.Combine(Store.Root, "Pictures", "honk.png");
            using (var preparation = new PicturePreparation())
            {
                for (int i = 0; i < 6; i++)
                {
                    string path = Path.Combine(Store.Root, "Pictures", "cache-" + i + ".png");
                    File.Copy(picture, path);
                    using (var prepared = preparation.Prepare(path, System.Threading.CancellationToken.None).GetAwaiter().GetResult())
                        check(prepared.Image.Width > 0, "Background picture preparation fully decodes picture " + i);
                }
                check(preparation.CachedBytes <= 16777216 && preparation.CachedPictures == 4, "Validated-picture cache respects its count and memory budgets");
                using (var cancel = new System.Threading.CancellationTokenSource())
                {
                    cancel.Cancel();
                    bool cancelled = false;
                    try
                    {
                        using (var prepared = preparation.Prepare(picture, cancel.Token).GetAwaiter().GetResult())
                        {
                        }
                    }
                    catch (OperationCanceledException) { cancelled = true; }
                    check(cancelled, "Picture preparation observes cancellation before publishing a window");
                }
                preparation.Dispose();
                check(preparation.CachedBytes == 0, "Shutdown releases the validated-picture cache");
            }
            string summary = Diagnostics.Summary();
            check(!summary.Contains(Store.Root) && !summary.Contains("external change"), "Explicit diagnostics exclude content and profile paths");
            string log = Path.Combine(Store.Root, "error.log");
            File.WriteAllText(log, new string('x', 1048576));
            for (int i = 0; i < 3; i++)
            {
                Program.Log(new IOException("rotation probe"));
                if (i < 2)
                    File.WriteAllText(log, new string('x', 1048576));
            }
            check(File.Exists(log + ".1") && File.Exists(log + ".2") && !File.Exists(log + ".3"), "Logs rotate within the three-file budget");
            StartupChecks(check);
        }
        static void StartupChecks(Action<bool, string> check)
        {
            string executable = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GooseOnTheLoose.exe"), root = Path.Combine(Store.Root, "secondary-instance");
            string previous = Environment.GetEnvironmentVariable("DESKTOPGOOSE2_DATA_DIR");
            try
            {
                Environment.SetEnvironmentVariable("DESKTOPGOOSE2_DATA_DIR", root);
                using (var quit = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(executable, "--quit") { UseShellExecute = false, CreateNoWindow = true }))
                {
                    check(quit.WaitForExit(6000) && quit.ExitCode == 0 && !Directory.Exists(root), "Quit without a running instance does not create or seed content");
                }
                using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true }))
                {
                    try
                    {
                        for (int i = 0; i < 200 && !File.Exists(Path.Combine(root, "runtime.json")); i++)
                            System.Threading.Thread.Sleep(25);
                        check(File.Exists(Path.Combine(root, "runtime.json")), "Isolated application starts and publishes diagnostic status");
                        string marker = Path.Combine(root, ".seeded");
                        File.Delete(marker);
                        using (var command = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(executable, "--honk") { UseShellExecute = false, CreateNoWindow = true }))
                            check(command.WaitForExit(6000) && command.ExitCode == 0 && !File.Exists(marker), "Secondary commands signal the instance without reseeding its profile");
                    }
                    finally { using (var quit = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(executable, "--quit") { UseShellExecute = false, CreateNoWindow = true })) quit.WaitForExit(6000); if (!process.WaitForExit(6000)) process.Kill(); }
                    check(process.HasExited, "Immediate CLI quit removes the isolated application's overlays and process");
                }
            }
            finally { Environment.SetEnvironmentVariable("DESKTOPGOOSE2_DATA_DIR", previous); }
        }
    }
}
