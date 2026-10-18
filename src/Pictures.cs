using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace SecondHonk
{
    public static class Pictures
    {
        public const string OldStockHash = "E32C10C3677CFB55618219A3F10777297F2843E6BAFC14014D5FE5C7B4B75CEA";
        public static byte[] ReadValidated(string path, CancellationToken cancel)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".gif")
                throw new InvalidDataException("Choose a PNG, JPEG or GIF picture.");
            byte[] bytes;
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (input.Length == 0 || input.Length > 8388608)
                    throw new InvalidDataException("Picture must be between 1 byte and 8 MB.");
                bytes = new byte[(int)input.Length];
                int at = 0;
                while (at < bytes.Length)
                {
                    cancel.ThrowIfCancellationRequested();
                    int n = input.Read(bytes, at, bytes.Length - at);
                    if (n == 0)
                        throw new EndOfStreamException("Picture is incomplete.");
                    at += n;
                }
            }
            using (var stream = new MemoryStream(bytes))
            using (var image = Image.FromStream(stream, true, true))
            {
                if (image.Width > 8192 || image.Height > 8192)
                    throw new InvalidDataException("Picture exceeds 8192 pixels per side.");
                bool animated = Array.IndexOf(image.FrameDimensionsList, FrameDimension.Time.Guid) >= 0;
                int count = animated ? image.GetFrameCount(FrameDimension.Time) : 1;
                if (count > 500 || (long)image.Width * image.Height * count > 134217728)
                    throw new InvalidDataException("Picture animation exceeds 500 frames or 128 million decoded pixels.");
                using (var decoded = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppPArgb))
                using (var g = Graphics.FromImage(decoded))
                for (int i = 0; i < count; i++)
                {
                    cancel.ThrowIfCancellationRequested();
                    if (animated)
                        image.SelectActiveFrame(FrameDimension.Time, i);
                    g.Clear(Color.Transparent);
                    g.DrawImageUnscaled(image, 0, 0);
                }
            }
            cancel.ThrowIfCancellationRequested();
            return bytes;
        }
        public static string Hash(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }
        public static string Publish(string original, byte[] bytes, string folder, CancellationToken cancel)
        {
            Directory.CreateDirectory(folder);
            string temp = Path.Combine(folder, ".dg2-import-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    for (int at = 0; at < bytes.Length; at += 65536)
                    {
                        cancel.ThrowIfCancellationRequested();
                        output.Write(bytes, at, Math.Min(65536, bytes.Length - at));
                    }
                    output.Flush(true);
                }
                string name = Path.GetFileNameWithoutExtension(original), extension = Path.GetExtension(original);
                for (int i = 1; ; i++)
                {
                    cancel.ThrowIfCancellationRequested();
                    string dest = Path.Combine(folder, name + (i == 1 ? "" : " (" + i + ")") + extension);
                    try
                    {
                        File.Move(temp, dest);
                        return dest;
                    }
                    catch (IOException) { if (!File.Exists(dest)) throw; }
                }
            }
            finally { if (File.Exists(temp)) try { File.Delete(temp); } catch (IOException) { } }
        }
        public static ImportResult Import(string[] paths, CancellationToken cancel)
        {
            var result = new ImportResult();
            foreach (string path in paths)
            {
                if (cancel.IsCancellationRequested)
                    break;
                try
                {
                    byte[] bytes = ReadValidated(path, cancel);
                    Publish(path, bytes, Path.Combine(Store.Root, "Pictures"), cancel);
                    result.Imported++;
                }
                catch (OperationCanceledException) { break; }
                catch (Exception e) { result.Errors.Add(Path.GetFileName(path) + ": " + e.Message); Program.Log(e); }
            }
            return result;
        }
        public static void MigrateStock(string bundled, string root)
        {
            string replacement = Path.Combine(bundled, "Packs", "Images", "honk.png");
            if (!File.Exists(replacement) || Hash(replacement) == OldStockHash)
                return;
            foreach (string folder in new[] { Path.Combine(root, "Pictures"), Path.Combine(root, "Images") })
            if (Directory.Exists(folder))
            foreach (string file in Directory.GetFiles(folder, "*.png"))
                try
                {
                    if (new FileInfo(file).Length > 8388608 || Hash(file) != OldStockHash)
                        continue;
                    string temp = Path.Combine(folder, ".dg2-stock-" + Guid.NewGuid().ToString("N") + ".tmp");
                    try
                    {
                        File.Copy(replacement, temp);
                        if (Hash(file) == OldStockHash)
                            File.Replace(temp, file, null);
                    }
                    finally { if (File.Exists(temp)) File.Delete(temp); }
                }
                catch (Exception e) { Program.Log(e); }
        }
    }
    public sealed class ImportResult
    {
        public int Imported; public readonly List<string> Errors = new List<string>();
    }
    public sealed class ImportProgress : Form
    {
        bool disposed;
        readonly CancellationTokenSource cancel = new CancellationTokenSource(); readonly System.Windows.Forms.Timer poll = new System.Windows.Forms.Timer { Interval = 50 }; readonly Task<ImportResult> task;
        public ImportResult Result
        {
            get; private set;
        }
        public ImportProgress(Func<CancellationToken, ImportResult> import)
        {
            Text = "Import pictures";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ShowInTaskbar = false;
            MinimizeBox = MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(320, 110);
            Controls.Add(new Label { Text = "Checking and importing your pictures…", Dock = DockStyle.Top, Height = 48, TextAlign = ContentAlignment.MiddleCenter });
            var button = new Button { Text = "Cancel", Width = 90, Location = new Point(115, 65) };
            button.Click += (s, e) => Close();
            Controls.Add(button);
            task = Task.Factory.StartNew(() => import(cancel.Token), cancel.Token, TaskCreationOptions.None, TaskScheduler.Default);
            poll.Tick += (s, e) => { if (!task.IsCompleted) return; if (task.Status == TaskStatus.RanToCompletion) Result = task.Result; else if (task.Exception != null) { Program.Log(task.Exception); Result = new ImportResult(); Result.Errors.Add(task.Exception.GetBaseException().Message); } Close(); };
            Shown += (s, e) => poll.Start();
            FormClosing += (s, e) => { if (!disposed) { cancel.Cancel(); poll.Stop(); } };
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                cancel.Cancel();
                poll.Dispose();
                if (task.IsCompleted)
                    cancel.Dispose();
                else
                    task.ContinueWith(t => { var ignored = t.Exception; cancel.Dispose(); });
            }
            base.Dispose(disposing);
        }
    }
}
