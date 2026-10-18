using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace SecondHonk
{
    sealed class PreparedPicture : IDisposable
    {
        public readonly MemoryStream Stream;
        public readonly Image Image;
        public PreparedPicture(byte[] bytes)
        {
            Stream = new MemoryStream(bytes, false);
            try
            {
                Image = Image.FromStream(Stream, true, true);
                using (var first = new Bitmap(Image.Width, Image.Height))
                using (var drawing = Graphics.FromImage(first))
                    drawing.DrawImageUnscaled(Image, 0, 0);
            }
            catch { Stream.Dispose(); throw; }
        }
        public void Dispose()
        {
            Image.Dispose();
            Stream.Dispose();
        }
    }
    sealed class PicturePreparation : IDisposable
    {
        sealed class Entry
        {
            public string Key; public byte[] Bytes;
        }
        readonly LinkedList<Entry> cache = new LinkedList<Entry>();
        readonly SemaphoreSlim gate = new SemaphoreSlim(1);
        readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        long bytes; bool disposed;
        public long CachedBytes
        {
            get
            {
                return Interlocked.Read(ref bytes);
            }
        }
        internal int CachedPictures
        {
            get
            {
                return cache.Count;
            }
        }
        public async Task<PreparedPicture> Prepare(string path, CancellationToken token)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(PicturePreparation));
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, lifetime.Token))
            {
                var cancel = linked.Token;
                return await Task.Run(async () =>
                {
                    await gate.WaitAsync(cancel).ConfigureAwait(false);
                    try
                    {
                        cancel.ThrowIfCancellationRequested();
                        var info = new FileInfo(path);
                        string key = info.FullName + "|" + info.LastWriteTimeUtc.Ticks + "|" + info.Length;
                        byte[] data = null;
                        for (var node = cache.First; node != null; node = node.Next)
                            if (node.Value.Key == key)
                            {
                                data = node.Value.Bytes;
                                cache.Remove(node);
                                cache.AddFirst(new Entry { Key = key, Bytes = data });
                                break;
                            }
                        if (data == null)
                        {
                            data = Pictures.ReadValidated(path, cancel);
                            cache.AddFirst(new Entry { Key = key, Bytes = data });
                            Interlocked.Add(ref bytes, data.Length);
                            while (cache.Count > 4 || bytes > 16777216)
                            {
                                Interlocked.Add(ref bytes, -cache.Last.Value.Bytes.Length);
                                cache.RemoveLast();
                            }
                        }
                        cancel.ThrowIfCancellationRequested();
                        var prepared = new PreparedPicture(data);
                        if (cancel.IsCancellationRequested)
                        {
                            prepared.Dispose();
                            cancel.ThrowIfCancellationRequested();
                        }
                        return prepared;
                    }
                    finally { if (lifetime.IsCancellationRequested) { cache.Clear(); Interlocked.Exchange(ref bytes, 0); } gate.Release(); }
                }, cancel).ConfigureAwait(false);
            }
        }
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            lifetime.Cancel();
            if (gate.Wait(0))
            {
                try
                {
                    cache.Clear();
                    Interlocked.Exchange(ref bytes, 0);
                }
                finally { gate.Release(); }
            }
        }
    }
}
