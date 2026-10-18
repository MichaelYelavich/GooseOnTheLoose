using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SecondHonk
{
    public sealed class ContentMonitor : IDisposable
    {
        sealed class Snapshot
        {
            public Content Content;
            public string Settings, Behavior;
            public Exception Error;
        }

        readonly FileSystemWatcher watcher;
        readonly World world;
        readonly string root = Store.Root;
        long due;
        volatile bool disposed, rearm;
        bool initializing = true;
        string settingsText, behaviorText;
        Task<Snapshot> pending;
        public event Action SettingsChanged;
        public ContentMonitor(World world)
        {
            this.world = world;
            watcher = new FileSystemWatcher(root)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };
            watcher.Changed += Changed;
            watcher.Created += Changed;
            watcher.Deleted += Changed;
            watcher.Renamed += Changed;
            watcher.Error += (s, e) =>
            {
                rearm = true;
                Queue();
            };
            watcher.EnableRaisingEvents = true;
            Reload();
            initializing = false;
        }

        void Changed(object sender, FileSystemEventArgs e)
        {
            if (!IgnoredEvent(e.Name))
                Queue();
        }

        internal static bool IgnoredEvent(string name)
        {
            return name != null && (name == "runtime.json" || name == "diagnostics.txt" || name.StartsWith("error.log", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase));
        }

        public void Queue()
        {
            if (!disposed)
                Interlocked.Exchange(ref due, DateTime.UtcNow.AddMilliseconds(650).Ticks);
        }

        public void Tick()
        {
            if (disposed)
                return;
            if (pending != null && pending.IsCompleted)
            {
                var snapshot = pending.GetAwaiter().GetResult();
                pending = null;
                Apply(snapshot);
            }

            long when = Interlocked.Read(ref due);
            if (pending == null && when > 0 && DateTime.UtcNow.Ticks >= when && Interlocked.CompareExchange(ref due, 0, when) == when)
                pending = Task.Run(() => Read());
        }

        Snapshot Read()
        {
            var snapshot = new Snapshot();
            try
            {
                if (!Directory.Exists(root) || !Directory.Exists(Path.Combine(root, "Notes")) || !Directory.Exists(Path.Combine(root, "Pictures")))
                    throw new DirectoryNotFoundException("Content folders are unavailable; previous content retained.");
                snapshot.Content = new Content(root);
                snapshot.Behavior = File.ReadAllText(Path.Combine(root, "Behaviors", "default.json"));
                snapshot.Settings = File.ReadAllText(Path.Combine(root, "settings.json"));
            }
            catch (Exception error)
            {
                snapshot.Error = error;
            }

            return snapshot;
        }

        public void Reload()
        {
            if (disposed)
                return;
            pending = null;
            Apply(Read());
        }

        void Apply(Snapshot snapshot)
        {
            if (disposed)
                return;
            if (snapshot.Error != null)
            {
                Queue();
                return;
            }

            if (rearm)
                try
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.EnableRaisingEvents = true;
                    rearm = false;
                }
                catch (IOException)
                {
                    Queue();
                }
                catch (UnauthorizedAccessException)
                {
                    Queue();
                }

            // Only this UI thread publishes the freshly loaded content snapshot.
            world.Content.Notes.Clear();
            world.Content.Notes.AddRange(snapshot.Content.Notes);
            world.Content.Images.Clear();
            world.Content.Images.AddRange(snapshot.Content.Images);
            world.Content.Warnings.Clear();
            world.Content.Warnings.AddRange(snapshot.Content.Warnings);
            try
            {
                if (snapshot.Behavior != behaviorText)
                {
                    var behavior = new JavaScriptSerializer().Deserialize<Behavior>(snapshot.Behavior);
                    if (behavior == null)
                        throw new InvalidDataException("Empty preset");
                    behavior.Validate();
                    world.ApplyBehavior(behavior);
                    behaviorText = snapshot.Behavior;
                }
            }
            catch (Exception error)
            {
                world.Content.Warnings.Add("Behavior: " + error.Message);
            }

            try
            {
                if (snapshot.Settings != settingsText)
                {
                    var data = new JavaScriptSerializer().Deserialize<SaveData>(snapshot.Settings);
                    if (data == null || data.Version != 2 || float.IsNaN(data.Scale) || float.IsInfinity(data.Scale) || data.Scale < 1 || data.Scale > 2 || !new[]
                    {
                        "None",
                        "Scarf",
                        "Sunhat"
                    }.Contains(data.Hat))
                        throw new InvalidDataException("Invalid settings; previous preferences retained.");
                    if (!initializing)
                    {
                        foreach (var field in typeof(SaveData).GetFields())
                            field.SetValue(world.Data, field.GetValue(data));
                        SettingsChanged?.Invoke();
                    }

                    settingsText = snapshot.Settings;
                }
            }
            catch (Exception error)
            {
                world.Content.Warnings.Add("Settings: " + error.Message);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            watcher.Dispose();
            pending = null;
        }
    }
}
