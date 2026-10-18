using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SecondHonk
{
    public static class Native
    {
        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr h, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll", SetLastError = true)] public static extern bool UpdateLayeredWindow(IntPtr h, IntPtr dc, ref Point pos, ref Size size, IntPtr source, ref Point origin, int key, ref Blend blend, int flags);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr h, IntPtr dc);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr dc);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
        delegate bool WindowCallback(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")] static extern bool EnumThreadWindows(uint thread, WindowCallback callback, IntPtr parameter);
        [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr window, uint command);
        [DllImport("user32.dll")] static extern bool PostMessage(IntPtr window, uint message, IntPtr wparam, IntPtr lparam);
        public static void CloseOwnedDialogs(IntPtr owner)
        {
            EnumThreadWindows(GetCurrentThreadId(), (window, p) => { IntPtr parent = GetWindow(window, 4); for (int i = 0; i < 8 && parent != IntPtr.Zero; i++) { if (parent == owner) { PostMessage(window, 0x10, IntPtr.Zero, IntPtr.Zero); break; } parent = GetWindow(parent, 4); } return true; }, IntPtr.Zero);
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Blend
        {
            public byte Op, Flags, Alpha, Format;
        }
        public static Icon MakeIcon()
        {
            using (var bitmap = new Bitmap(64, 64))
            {
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.FromArgb(55, 70, 55));
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    var goose = new Goose(new V(29, 52));
                    goose.HeadDirection = goose.Direction = -.3f;
                    goose.LeftFoot = new V(24, 53);
                    goose.RightFoot = new V(32, 55);
                    Art.Goose(g, goose, 1.1f, "None", false, true);
                }
                IntPtr h = bitmap.GetHicon();
                Icon result = (Icon)Icon.FromHandle(h).Clone();
                DestroyIcon(h);
                return result;
            }
        }
    }
    public sealed class DesktopOverlay : Form
    {
        readonly World world;
        Bitmap frame;
        readonly FootprintRenderer footprints = new FootprintRenderer();
        readonly bool interaction;
        DesktopObject dragging; V dragOffset, lastPointer; DateTime lastDrag; bool resizing;
        public Rectangle Area
        {
            get; private set;
        }
        public DesktopOverlay(World world, Rectangle area) : this(world, area, false) { }
        public DesktopOverlay(World world, Rectangle area, bool interactive)
        {
            this.world = world;
            interaction = interactive;
            Area = area;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Bounds = area;
            TopMost = true;
            Text = interactive ? "Desktop Goose 2 — toys" : "Desktop Goose 2 — overlay";
        }
        protected override CreateParams CreateParams
        {
            get
            {
                var p = base.CreateParams;
                p.ExStyle |= 0x80000 | 0x80 | 0x08000000;
                if (!interaction)
                    p.ExStyle |= 0x20;
                return p;
            }
        }
        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }
        Rectangle Around(V p, int radius)
        {
            return new Rectangle((int)p.X - radius, (int)p.Y - radius, radius * 2, radius * 2);
        }
        public void RenderFrame()
        {
            if (IsDisposed)
                return;
            Rectangle crop = Around(world.Goose.Pos, (int)(85 * world.Scale));
            if (world.Data.Companion)
                crop = Rectangle.Union(crop, Around(world.Friend.Pos, (int)(85 * world.Scale)));
            if (world.BallVisible)
                crop = Rectangle.Union(crop, Around(world.BallPos, 32));
            if (world.StickVisible)
                crop = Rectangle.Union(crop, Around(world.CarryingStick ? world.Beak(world.Goose) : world.StickPos, 40));
            foreach (Mark mark in world.Marks)
                crop = Rectangle.Union(crop, Around(mark.Pos, 32));
            foreach (var o in world.Objects)
                crop = Rectangle.Union(crop, new Rectangle((int)(o.Pos.X - o.Size.X / 2 - 28), (int)(o.Pos.Y - o.Size.Y / 2 - 28), (int)o.Size.X + 56, (int)o.Size.Y + 56));
            if (world.Exploding)
                crop = Rectangle.Inflate(crop, 140, 140);
            if (world.DismissRemaining > 0)
                crop = Rectangle.Union(crop, Around(Art.DismissPosition(world) + new V(36, 18), 55));
            crop.Intersect(new Rectangle(0, 0, Area.Width, Area.Height));
            if (crop.Width <= 0 || crop.Height <= 0)
            {
                if (Visible)
                    Hide();
                return;
            }
            int width = Math.Max(64, ((crop.Width + 63) / 64) * 64), height = Math.Max(64, ((crop.Height + 63) / 64) * 64);
            if (frame == null || frame.Width != width || frame.Height != height)
            {
                if (frame != null)
                    frame.Dispose();
                frame = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            }
            using (var g = Graphics.FromImage(frame))
            {
                g.Clear(Color.Transparent);
                g.TranslateTransform(-crop.X, -crop.Y);
                if (interaction)
                    Art.Interactions(g, world);
                else
                    Art.Scene(g, world, footprints);
            }
            IntPtr screen = Native.GetDC(IntPtr.Zero), memory = Native.CreateCompatibleDC(screen), bitmap = IntPtr.Zero, previous = IntPtr.Zero;
            try
            {
                bitmap = frame.GetHbitmap(Color.FromArgb(0));
                previous = Native.SelectObject(memory, bitmap);
                Point position = new Point(Area.X + crop.X, Area.Y + crop.Y), origin = Point.Empty;
                Size size = frame.Size;
                var blend = new Native.Blend { Alpha = 255, Format = 1 };
                if (!Native.UpdateLayeredWindow(Handle, screen, ref position, ref size, memory, ref origin, 0, ref blend, 2))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                if (!Visible)
                    Show();
            }
            finally { if (previous != IntPtr.Zero) Native.SelectObject(memory, previous); if (bitmap != IntPtr.Zero) Native.DeleteObject(bitmap); Native.DeleteDC(memory); Native.ReleaseDC(IntPtr.Zero, screen); }
        }
        V Pointer()
        {
            Point p = Cursor.Position;
            return new V(p.X - Area.X, p.Y - Area.Y);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
                BeginDrag(Pointer());
        }
        internal bool BeginDrag(V p)
        {
            if (!interaction || world.Exploding)
                return false;
            for (int i = world.Objects.Count - 1; i >= 0; i--)
            {
                var o = world.Objects[i];
                V corner = o.Pos + new V(o.Size.X / 2, -o.Size.Y / 2);
                if (world.Editing && V.Distance(p, corner) < 12)
                {
                    world.RemoveObject(o);
                    return true;
                }
                bool resize = world.Editing && o.Surface && V.Distance(p, o.Pos + o.Size / 2) < 13;
                bool move = world.Editing && o.Surface && V.Distance(p, o.Pos - new V(0, o.Size.Y / 2)) < 14;
                if ((!o.Surface && o.Contains(p)) || move || resize)
                {
                    dragging = o;
                    if (o.Kind == ToyKind.Stick)
                        world.Cancel();
                    o.Dragging = true;
                    o.Carried = false;
                    world.GrabRemaining = 0;
                    resizing = resize;
                    dragOffset = p - o.Pos;
                    lastPointer = p;
                    lastDrag = DateTime.UtcNow;
                    Capture = true;
                    return true;
                }
            }
            return false;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            MoveDrag(Pointer(), (float)(DateTime.UtcNow - lastDrag).TotalSeconds);
        }
        internal void MoveDrag(V p, float dt)
        {
            if (dragging == null)
                return;
            if (resizing)
            {
                V size = (p - dragging.Pos) * 2;
                dragging.Size = new V(Math.Max(70, Math.Min(600, size.X)), Math.Max(45, Math.Min(400, size.Y)));
                dragging.Pos = world.ConstrainObject(dragging, dragging.Pos);
            }
            else
            {
                dragging.Pos = world.ConstrainObject(dragging, p - dragOffset);
                dragging.Velocity = dt > .001f ? (p - lastPointer) / dt : new V();
                if (dragging.Velocity.Length > 900)
                    dragging.Velocity = dragging.Velocity.Unit * 900;
            }
            lastPointer = p;
            lastDrag = DateTime.UtcNow;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            ReleaseDrag();
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!Capture)
                ReleaseDrag();
        }
        public void ReleaseDrag()
        {
            if (dragging != null)
            {
                dragging.Dragging = false;
                if (dragging.Surface || DateTime.UtcNow - lastDrag > TimeSpan.FromMilliseconds(150))
                    dragging.Velocity = new V();
                dragging = null;
            }
            Capture = false;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseDrag();
                footprints.Dispose();
                if (frame != null)
                {
                    frame.Dispose();
                    frame = null;
                }
            }
            base.Dispose(disposing);
        }
    }
    public sealed class GooseWindow : Form
    {
        Image image;
        MemoryStream imageStream;
        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }
        public GooseWindow(string note, string imagePath)
        {
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = true;
            TopMost = true;
            BackColor = Color.White;
            MinimumSize = new Size(220, 150);
            ClientSize = new Size(350, 210);
            Text = "Goose Notepad";
            if (!String.IsNullOrEmpty(imagePath))
                try
                {
                    imageStream = new MemoryStream(Pictures.ReadValidated(imagePath, System.Threading.CancellationToken.None));
                    image = Image.FromStream(imageStream, true, true);
                    Text = "A gift from your goose";
                    ClientSize = new Size(360, 280);
                    Controls.Add(new PictureBox { Image = image, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill, BackColor = Color.White });
                }
                catch (Exception) { note = "Couldn't open that picture.\r\nHave a honk instead."; }
            if (image == null)
                Controls.Add(new TextBox { Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, Dock = DockStyle.Fill, Text = note.Replace("\r\n", "\n").Replace("\n", "\r\n"), Font = new Font("Consolas", 12), BackColor = Color.White, ForeColor = Color.Black, ScrollBars = ScrollBars.Vertical });
        }
        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            finally { if (disposing && image != null) { image.Dispose(); image = null; } if (disposing && imageStream != null) { imageStream.Dispose(); imageStream = null; } }
        }
    }
    public sealed class HonkAudio : IDisposable
    {
        readonly MemoryStream wave;
        readonly SoundPlayer player;
        readonly List<string> honks = new List<string>(), opened = new List<string>();
        readonly List<SoundPlayer> steps = new List<SoundPlayer>();
        readonly Random random = new Random();
        string bite, mud;
        MemoryStream popWave; SoundPlayer popPlayer;
        public int SuccessfulPlays
        {
            get; private set;
        }
        public string Backend
        {
            get
            {
                return honks.Count > 0 ? "Original Desktop Goose samples (WinMM)" : "Synthesized fallback";
            }
        }
        [DllImport("winmm.dll", CharSet = CharSet.Unicode)] static extern uint mciSendString(string command, StringBuilder result, int length, IntPtr callback);
        string Open(string folder, string filename, string alias)
        {
            string path = Path.Combine(folder, filename);
            if (File.Exists(path) && mciSendString("open \"" + path + "\" type mpegvideo alias " + alias, null, 0, IntPtr.Zero) == 0)
            {
                opened.Add(alias);
                return alias;
            }
            return null;
        }
        bool PlayClip(string alias, int volume)
        {
            if (alias == null)
                return false;
            mciSendString("stop " + alias, null, 0, IntPtr.Zero);
            mciSendString("seek " + alias + " to start", null, 0, IntPtr.Zero);
            mciSendString("setaudio " + alias + " volume to " + volume, null, 0, IntPtr.Zero);
            bool success = mciSendString("play " + alias, null, 0, IntPtr.Zero) == 0;
            if (success)
                SuccessfulPlays++;
            return success;
        }
        public HonkAudio()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds");
            for (int i = 1; i <= 4; i++)
            {
                string alias = Open(folder, "Honk" + i + ".mp3", "dg2_honk" + i);
                if (alias != null)
                    honks.Add(alias);
            }
            bite = Open(folder, "BITE.mp3", "dg2_bite");
            mud = Open(folder, "MudSquith.mp3", "dg2_mud");
            for (int i = 1; i <= 3; i++)
            {
                string path = Path.Combine(folder, "Pat" + i + ".wav");
                if (File.Exists(path))
                    try
                    {
                        var step = new SoundPlayer(path);
                        step.Load();
                        steps.Add(step);
                    }
                    catch (Exception) { }
            }
            wave = new MemoryStream();
            var writer = new BinaryWriter(wave);
            int rate = 22050, count = (int)(rate * .26);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + count * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(rate);
            writer.Write(rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(count * 2);
            double phase = 0;
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / rate, u = (double)i / count;
                phase += 2 * Math.PI * (380 - 110 * u + 24 * Math.Sin(t * 36)) / rate;
                double envelope = Math.Min(1, u * 22) * Math.Pow(1 - u, .7);
                double value = (Math.Sin(phase) + .4 * Math.Sin(phase * 2) + .2 * Math.Sin(phase * 3)) * envelope * .21;
                writer.Write((short)(value * short.MaxValue));
            }
            wave.Position = 0;
            player = new SoundPlayer(wave);
            player.Load();
        }
        public void Play()
        {
            if (honks.Count > 0 && PlayClip(honks[random.Next(honks.Count)], 650))
                return;
            try
            {
                player.Play();
            }
            catch (Exception) { }
        }
        public void Bite()
        {
            if (!PlayClip(bite, 180))
                Play();
        }
        public void Mud()
        {
            PlayClip(mud, 150);
        }
        public void Step()
        {
            if (steps.Count > 0)
                try
                {
                    steps[random.Next(steps.Count)].Play();
                }
                catch (Exception) { }
        }
        public void Pop()
        {
            try
            {
                if (popPlayer == null)
                {
                    popWave = new MemoryStream();
                    var writer = new BinaryWriter(popWave);
                    int rate = 22050, count = 3307;
                    writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(36 + count * 2);
                    writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                    writer.Write(16);
                    writer.Write((short)1);
                    writer.Write((short)1);
                    writer.Write(rate);
                    writer.Write(rate * 2);
                    writer.Write((short)2);
                    writer.Write((short)16);
                    writer.Write(Encoding.ASCII.GetBytes("data"));
                    writer.Write(count * 2);
                    for (int i = 0; i < count; i++)
                    {
                        double t = (double)i / rate, u = (double)i / count;
                        writer.Write((short)(Math.Sin(2 * Math.PI * (260 * t - 500 * t * t)) * Math.Exp(-u * 7) * 3500));
                    }
                    popWave.Position = 0;
                    popPlayer = new SoundPlayer(popWave);
                    popPlayer.Load();
                }
                popPlayer.Play();
            }
            catch (Exception) { }
        }
        public void Dispose()
        {
            foreach (string alias in opened)
                mciSendString("close " + alias, null, 0, IntPtr.Zero);
            foreach (var step in steps)
                step.Dispose();
            if (popPlayer != null)
                popPlayer.Dispose();
            if (popWave != null)
                popWave.Dispose();
            player.Dispose();
            wave.Dispose();
        }
    }
}
