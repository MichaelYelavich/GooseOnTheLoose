using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace SecondHonk
{
    // Stamps are rasterized once at landing size, independent of the moving overlay crop.
    public sealed class FootprintRenderer : IDisposable
    {
        readonly Dictionary<Mark, Bitmap> stamps = new Dictionary<Mark, Bitmap>();
        readonly HashSet<Mark> alive = new HashSet<Mark>();
        readonly List<Mark> retired = new List<Mark>();
        readonly ImageAttributes attrs = new ImageAttributes();
        readonly ColorMatrix matrix = new ColorMatrix();
        public int Count
        {
            get
            {
                return stamps.Count;
            }
        }

        public void Draw(Graphics g, World world)
        {
            bool changed = world.Marks.Count != stamps.Count;
            if (!changed)
                foreach (var mark in world.Marks)
                    if (!stamps.ContainsKey(mark))
                    {
                        changed = true;
                        break;
                    }

            if (changed)
            {
                alive.Clear();
                foreach (var mark in world.Marks)
                    alive.Add(mark);
                retired.Clear();
                foreach (var mark in stamps.Keys)
                    if (!alive.Contains(mark))
                        retired.Add(mark);
                foreach (var old in retired)
                {
                    stamps[old].Dispose();
                    stamps.Remove(old);
                }
            }

            var state = g.Save();
            g.PixelOffsetMode = PixelOffsetMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            try
            {
                foreach (var mark in world.Marks)
                {
                    Bitmap stamp;
                    if (!stamps.TryGetValue(mark, out stamp))
                    {
                        stamp = Create(mark);
                        stamps.Add(mark, stamp);
                    }

                    float fade = Math.Max(0, Math.Min(1, (world.Behavior.FootprintSeconds - mark.Age) / 2));
                    {
                        matrix.Matrix33 = fade;
                        attrs.SetColorMatrix(matrix);
                        g.DrawImage(stamp, new Rectangle((int)Math.Round(mark.Pos.X) - 32, (int)Math.Round(mark.Pos.Y) - 32, 64, 64), 0, 0, 64, 64, GraphicsUnit.Pixel, attrs);
                    }
                }
            }
            finally
            {
                g.Restore(state);
            }
        }

        static Bitmap Create(Mark mark)
        {
            var bitmap = new Bitmap(64, 64, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TranslateTransform(32, 32);
                g.RotateTransform(mark.Angle * 180 / (float)Math.PI);
                float s = mark.Scale;
                Color color = mark.Coating == Coating.Wet ? Color.FromArgb(100, 68, 130, 160) : Color.FromArgb(145, 107, 78, 43);
                using (var b = new SolidBrush(color))
                {
                    g.FillEllipse(b, -4 * s, -2 * s, 8 * s, 4 * s);
                    g.FillEllipse(b, 2 * s, -4 * s, 3 * s, 3 * s);
                    g.FillEllipse(b, 3 * s, -s, 3 * s, 3 * s);
                    g.FillEllipse(b, 2 * s, 2 * s, 3 * s, 3 * s);
                }
            }

            return bitmap;
        }

        public void Dispose()
        {
            foreach (var stamp in stamps.Values)
                stamp.Dispose();
            stamps.Clear();
            attrs.Dispose();
        }
    }
}
