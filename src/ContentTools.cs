using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Web.Script.Serialization;

namespace SecondHonk
{
    public static class UserContent
    {
        public static void Initialize(string bundled)
        {
            Initialize(bundled, null, true);
        }
        public static void Initialize(string bundled, string legacyRoot, bool honorOverride)
        {
            string overrideRoot = honorOverride ? Environment.GetEnvironmentVariable("DESKTOPGOOSE2_DATA_DIR") : null;
            if (!String.IsNullOrWhiteSpace(overrideRoot))
                Store.Root = Path.GetFullPath(overrideRoot);
            Directory.CreateDirectory(Store.Root);
            foreach (string folder in new[] { "Pictures", "Notes", "Behaviors" })
                Directory.CreateDirectory(Path.Combine(Store.Root, folder));
            string settings = Path.Combine(Store.Root, "settings.json"), legacy = Path.Combine(legacyRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopGoose2"), "settings.json");
            if (!File.Exists(settings) && File.Exists(legacy))
                File.Copy(legacy, settings);
            if (!File.Exists(settings))
                Store.Save(new SaveData());
            string defaults = new JavaScriptSerializer().Serialize(new Behavior());
            Seed(Path.Combine(Store.Root, "Behaviors", "default.json"), "{\r\n  \"_comment\": \"Edit these numbers to change your goose. Ranges and explanations are in README.txt. Zero activity weight disables that activity.\",\r\n  " + defaults.Substring(1, defaults.Length - 2).Replace(",", ",\r\n  ") + "\r\n}");
            // A marker prevents removed sample content being recreated on every upgrade.
            string marker = Path.Combine(Store.Root, ".seeded");
            if (!File.Exists(marker))
            {
                if (Directory.GetFiles(Path.Combine(Store.Root, "Notes")).Length == 0)
                {
                    var initial = new Content(Path.Combine(bundled, "Packs"));
                    for (int i = 0; i < initial.Notes.Count; i++)
                        Seed(Path.Combine(Store.Root, "Notes", "Goose-" + (i + 1) + ".txt"), initial.Notes[i]);
                }
                string sample = Path.Combine(bundled, "Packs", "Images", "honk.png"), target = Path.Combine(Store.Root, "Pictures", "honk.png");
                if (File.Exists(sample) && !File.Exists(target))
                    File.Copy(sample, target);
                Seed(marker, "1");
            }
            Pictures.MigrateStock(bundled, Store.Root);
            Seed(Path.Combine(Store.Root, "README.txt"), "DESKTOP GOOSE 2 â€” YOUR CONTENT\r\n\r\nPictures: PNG, JPEG or GIF, up to 8 MB and 8192 pixels per side.\r\nNotes: ordinary .txt files, up to 1000 characters.\r\nChanges reload automatically after saving. Invalid or incomplete pictures are skipped.\r\n\r\nBehaviors/default.json is plain JSON (no script code). Comments belong here, not in the JSON.\r\nPrankMinSeconds/PrankMaxSeconds: 5â€“3600 seconds, max >= min.\r\nMudWeight/NoteWeight/PictureWeight/ChaseWeight/ToyWeight: 0â€“20; zero disables that choice.\r\nRandom cursor attacks also require the tray option to be enabled.\r\nToyApproachSpeed: 20â€“400. NudgeSpeed: 0â€“600.\r\nTrailSteps: 0â€“100 (default 20). FootprintSeconds: 1â€“60 (default 10).\r\nAn invalid preset keeps the last valid behavior. Use Reload content to retry.\r\n\r\nsettings.json stores tray preferences. Desktop objects and delivered windows are never saved.\r\nUninstall keeps this folder. You can delete it yourself when you no longer want the content.\r\n");
        }
        static void Seed(string path, string value)
        {
            if (!File.Exists(path))
                File.WriteAllText(path, value);
        }
        public static string CopyUnique(string source, string folder)
        {
            Directory.CreateDirectory(folder);
            string name = Path.GetFileNameWithoutExtension(source), ext = Path.GetExtension(source), dest = Path.Combine(folder, name + ext);
            for (int i = 2; File.Exists(dest); i++)
                dest = Path.Combine(folder, name + " (" + i + ")" + ext);
            File.Copy(source, dest);
            return dest;
        }
        public static void ImportPictures(IWin32Window owner, Func<bool> closing)
        {
            using (var dialog = new OpenFileDialog { Title = "Pictures for your goose", Filter = "Pictures|*.png;*.jpg;*.jpeg;*.gif", Multiselect = true })
            if (dialog.ShowDialog(owner) == DialogResult.OK && !closing())
            {
                string[] files = dialog.FileNames;
                ShowImport(owner, closing, token => Pictures.Import(files, token));
            }
        }
        public static void ShowImport(IWin32Window owner, Func<bool> closing, Func<System.Threading.CancellationToken, ImportResult> import)
        {
            using (var progress = new ImportProgress(import))
            {
                progress.ShowDialog(owner);
                if (!closing() && progress.Result != null && progress.Result.Errors.Count > 0)
                    MessageBox.Show(owner, progress.Result.Imported + " file(s) imported. Skipped files:\r\n\r\n" + String.Join("\r\n", progress.Result.Errors.Take(12)) + (progress.Result.Errors.Count > 12 ? "\r\nâ€¦ See error.log for the remaining files." : ""), "Some files were skipped", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        public static void ImportLegacy(IWin32Window owner, Func<bool> closing)
        {
            using (var dialog = new FolderBrowserDialog { Description = "Choose the older goose folder or its content folder" })
            if (dialog.ShowDialog(owner) == DialogResult.OK && !closing())
            {
                string folder = dialog.SelectedPath;
                ShowImport(owner, closing, token => ImportLegacyFolder(folder, token));
            }
        }
        public static ImportResult ImportLegacyFolder(string selected)
        {
            return ImportLegacyFolder(selected, System.Threading.CancellationToken.None);
        }
        public static ImportResult ImportLegacyFolder(string selected, System.Threading.CancellationToken cancel)
        {
            var result = new ImportResult();
            var folders = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in new[] { selected, Path.Combine(selected, "Packs"), Path.Combine(selected, "Assets") })
            foreach (string child in new[] { "", "Images", "Pictures", "Notes", "Memes", "Text" })
                folders.Add(Path.Combine(root, child));
            foreach (string folder in folders)
            if (Directory.Exists(folder))
            foreach (string file in Directory.GetFiles(folder))
                try
                {
                    cancel.ThrowIfCancellationRequested();
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (new[] { ".png", ".jpg", ".jpeg", ".gif" }.Contains(ext))
                    {
                        Pictures.Publish(file, Pictures.ReadValidated(file, cancel), Path.Combine(Store.Root, "Pictures"), cancel);
                        result.Imported++;
                    }
                    else if (ext == ".txt")
                    {
                        if (new FileInfo(file).Length <= 65536 && File.ReadAllText(file).Length <= 1000)
                            CopyUnique(file, Path.Combine(Store.Root, "Notes"));
                    }
                    else if (ext == ".json" && new FileInfo(file).Length <= 131072)
                    {
                        var pack = new JavaScriptSerializer().Deserialize<ContentPack>(File.ReadAllText(file));
                        if (pack != null && pack.notes != null)
                        foreach (string note in pack.notes.Take(100).Where(n => !String.IsNullOrWhiteSpace(n) && n.Length <= 1000))
                            File.WriteAllText(Path.Combine(Store.Root, "Notes", "Imported-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".txt"), note);
                    }
                }
                catch (OperationCanceledException) { return result; }
                catch (Exception e) { result.Errors.Add(Path.GetFileName(file) + ": " + e.Message); Program.Log(e); }
            return result;
        }
    }
    public sealed class ContentMonitor : IDisposable
    {
        readonly FileSystemWatcher watcher; readonly World world; long due; bool disposed, initializing = true; string settingsText, behaviorText; public event Action SettingsChanged;
        public ContentMonitor(World w)
        {
            world = w;
            watcher = new FileSystemWatcher(Store.Root) { IncludeSubdirectories = true, NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName };
            watcher.Changed += Changed;
            watcher.Created += Changed;
            watcher.Deleted += Changed;
            watcher.Renamed += Changed;
            watcher.Error += (s, e) => Queue();
            watcher.EnableRaisingEvents = true;
            Reload();
            initializing = false;
        }
        void Changed(object s, FileSystemEventArgs e)
        {
            if (e.Name == "runtime.json" || e.Name == "error.log" || e.Name.EndsWith(".tmp") || e.Name.EndsWith(".bak"))
                return;
            Queue();
        }
        public void Queue()
        {
            System.Threading.Interlocked.Exchange(ref due, DateTime.UtcNow.AddMilliseconds(650).Ticks);
        }
        public void Tick()
        {
            long when = System.Threading.Interlocked.Read(ref due);
            if (when > 0 && DateTime.UtcNow.Ticks >= when && System.Threading.Interlocked.CompareExchange(ref due, 0, when) == when)
                Reload();
        }
        public void Reload()
        {
            try
            {
                var fresh = new Content(Store.Root);
                world.Content.Notes.Clear();
                world.Content.Notes.AddRange(fresh.Notes);
                world.Content.Images.Clear();
                world.Content.Images.AddRange(fresh.Images);
                world.Content.Warnings.Clear();
                world.Content.Warnings.AddRange(fresh.Warnings);
            }
            catch (IOException) { Queue(); }
            try
            {
                string json = File.ReadAllText(Path.Combine(Store.Root, "Behaviors", "default.json"));
                if (json != behaviorText)
                {
                    var b = new JavaScriptSerializer().Deserialize<Behavior>(json);
                    if (b == null)
                        throw new InvalidDataException("Empty preset");
                    b.Validate();
                    world.ApplyBehavior(b);
                    behaviorText = json;
                }
            }
            catch (Exception e) { world.Content.Warnings.Add("Behavior: " + e.Message); }
            try
            {
                string json = File.ReadAllText(Path.Combine(Store.Root, "settings.json"));
                if (json != settingsText)
                {
                    var s = new JavaScriptSerializer().Deserialize<SaveData>(json);
                    if (s == null || s.Version != 2 || float.IsNaN(s.Scale) || float.IsInfinity(s.Scale) || s.Scale < 1 || s.Scale > 2 || !new[] { "None", "Scarf", "Sunhat" }.Contains(s.Hat))
                        throw new InvalidDataException("Invalid settings; previous preferences retained.");
                    if (!initializing)
                    {
                        foreach (var field in typeof(SaveData).GetFields())
                            field.SetValue(world.Data, field.GetValue(s));
                        if (SettingsChanged != null)
                            SettingsChanged();
                    }
                    settingsText = json;
                }
            }
            catch (Exception e) { world.Content.Warnings.Add("Settings: " + e.Message); }
        }
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            watcher.Dispose();
        }
    }
    public sealed class NoteEditor : Form
    {
        readonly ListBox list = new ListBox { Dock = DockStyle.Left, Width = 150 }; readonly TextBox text = new TextBox { Multiline = true, Dock = DockStyle.Fill, MaxLength = 1000, ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 11) }; string current; bool silent;
        public NoteEditor()
        {
            Text = "Goose notes";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(540, 320);
            MinimumSize = new Size(420, 260);
            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(6) };
            var add = new Button { Text = "New note", AutoSize = true };
            var save = new Button { Text = "Save", AutoSize = true };
            bar.Controls.Add(add);
            bar.Controls.Add(save);
            Controls.Add(text);
            Controls.Add(list);
            Controls.Add(bar);
            list.SelectedIndexChanged += (s, e) => { Save(); current = list.SelectedItem as string; text.Text = current == null ? "" : File.ReadAllText(Path.Combine(Store.Root, "Notes", current)); };
            add.Click += (s, e) => { Save(); string name = "Note-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".txt"; File.WriteAllText(Path.Combine(Store.Root, "Notes", name), "Honk."); list.Items.Add(name); list.SelectedItem = name; };
            save.Click += (s, e) => Save();
            FormClosing += (s, e) => Save();
            foreach (string path in Directory.GetFiles(Path.Combine(Store.Root, "Notes"), "*.txt"))
                list.Items.Add(Path.GetFileName(path));
            if (list.Items.Count > 0)
                list.SelectedIndex = 0;
        }
        public void CloseForShutdown()
        {
            silent = true;
            Close();
        }
        void Save()
        {
            if (current == null)
                return;
            try
            {
                File.WriteAllText(Path.Combine(Store.Root, "Notes", current), text.Text);
            }
            catch (Exception e) { if (silent) Program.Log(e); else MessageBox.Show(this, e.Message, "Could not save note"); }
        }
    }
    public sealed class ShutdownClock
    {
        public float Held, EffectTime; public bool Animating, Finished;
        public bool Advance(bool escape, float dt, bool reduced)
        {
            if (Finished)
                return false;
            if (Animating)
            {
                EffectTime += Math.Max(0, dt);
                Finished = EffectTime >= (reduced ? .25f : .8f);
                return false;
            }
            Held = escape ? Held + Math.Max(0, dt) : 0;
            if (Held >= 5)
            {
                Held = 5;
                Animating = true;
                return true;
            }
            return false;
        }
    }
}
