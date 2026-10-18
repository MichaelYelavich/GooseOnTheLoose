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
    public sealed class DesktopOverlay : Form
    {
        readonly World world;
        LayeredBuffer buffer;
        readonly FootprintRenderer footprints = new FootprintRenderer();
        readonly bool interaction;
        internal event Action Changed;
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
            Text = interactive ? "Goose on the Loose — toys" : "Goose on the Loose — overlay";
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
            Rectangle crop = Rectangle.Empty;
            if (!interaction)
            {
                crop = Around(world.Goose.Pos, (int)(85 * world.Scale));
                if (world.Data.Companion)
                    crop = Rectangle.Union(crop, Around(world.Friend.Pos, (int)(85 * world.Scale)));
                foreach (Mark mark in world.Marks)
                    crop = Rectangle.Union(crop, Around(mark.Pos, 32));
                if (world.Exploding)
                    crop = Rectangle.Inflate(crop, 140, 140);
                if (world.DismissRemaining > 0)
                    crop = Rectangle.Union(crop, Around(Art.DismissPosition(world) + new V(36, 18), 55));
            }
            foreach (var o in world.Objects)
            {
                if (interaction && o.Surface && !world.Editing)
                    continue;
                if (!interaction && !o.Surface)
                    continue;
                var bounds = new Rectangle((int)(o.Pos.X - o.Size.X / 2 - 28), (int)(o.Pos.Y - o.Size.Y / 2 - 28), (int)o.Size.X + 56, (int)o.Size.Y + 56);
                crop = crop.IsEmpty ? bounds : Rectangle.Union(crop, bounds);
            }
            crop.Intersect(new Rectangle(0, 0, Area.Width, Area.Height));
            if (crop.Width <= 0 || crop.Height <= 0)
            {
                if (Visible)
                    Hide();
                return;
            }
            int width = Math.Max(64, ((crop.Width + 63) / 64) * 64), height = Math.Max(64, ((crop.Height + 63) / 64) * 64);
            if (buffer == null || buffer.Frame.Width != width || buffer.Frame.Height != height)
            {
                if (buffer != null)
                    buffer.Dispose();
                buffer = new LayeredBuffer(width, height);
            }
            using (var g = Graphics.FromImage(buffer.Frame))
            {
                g.Clear(Color.Transparent);
                g.TranslateTransform(-crop.X, -crop.Y);
                if (interaction)
                    Art.Interactions(g, world);
                else
                    Art.Scene(g, world, footprints);
            }
            buffer.Present(Handle, new Point(Area.X + crop.X, Area.Y + crop.Y));
            if (!Visible)
                Show();
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
                    Changed?.Invoke();
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
                    Changed?.Invoke();
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
            Changed?.Invoke();
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
                Changed?.Invoke();
            }
            Capture = false;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ReleaseDrag();
                footprints.Dispose();
                if (buffer != null)
                {
                    buffer.Dispose();
                    buffer = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
