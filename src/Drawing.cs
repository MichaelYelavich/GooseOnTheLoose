using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SecondHonk
{
    public static class Art
    {
        static readonly Font sleepyFont = new Font("Segoe UI", 11, FontStyle.Regular, GraphicsUnit.Pixel);
        static PointF P(V v)
        {
            return new PointF(v.X, v.Y);
        }
        static void Line(Graphics g, Color color, float width, V a, V b)
        {
            using (var pen = new Pen(color, width) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLine(pen, P(a), P(b));
        }
        static void Circle(Graphics g, Color color, V p, float radius)
        {
            using (var brush = new SolidBrush(color))
                g.FillEllipse(brush, p.X - radius, p.Y - radius, radius * 2, radius * 2);
        }
        public static V DismissPosition(World world)
        {
            return new V(Math.Max(8, Math.Min(world.Width - 80, world.Goose.Pos.X - 36)), Math.Max(8, Math.Min(world.Height - 44, world.Goose.Pos.Y - 90 * world.Scale)));
        }
        public static void Goose(Graphics g, Goose goose, float scale, string hat, bool friend, bool reduced)
        {
            V pos = goose.Pos, forward = V.Direction(goose.Direction), headForward = V.Direction(goose.HeadDirection), side = V.Direction(goose.HeadDirection + (float)Math.PI / 2);
            V body = pos + new V(0, -14 * scale), under = pos + new V(0, -9 * scale), neckBase = body + forward * (15 * scale);
            bool asleep = goose.Task == Activity.Sleep && goose.Velocity.Length < 5;
            float neckHeight = asleep ? 8 : 20 - 10 * goose.Neck, neckForward = asleep ? -4 : 3 + 13 * goose.Neck;
            V head = neckBase + headForward * (neckForward * scale) + new V(0, -neckHeight * scale), head1 = head + headForward * (3 * scale) + new V(0, scale), head2 = head1 + headForward * (5 * scale);
            Color white = friend ? Color.FromArgb(140, 132, 111) : Color.White, outline = friend ? Color.FromArgb(91, 92, 81) : Color.FromArgb(204, 207, 202), orange = friend ? Color.FromArgb(51, 48, 42) : Color.FromArgb(247, 163, 46), neck = friend ? Color.FromArgb(39, 44, 42) : Color.White;
            if (!goose.Swimming)
            {
                Circle(g, orange, goose.LeftFoot, 4 * scale);
                Circle(g, orange, goose.RightFoot, 4 * scale);
            }
            using (var brush = new SolidBrush(Color.FromArgb(35, 0, 0, 0)))
                g.FillEllipse(brush, pos.X - 24 * scale, pos.Y - 7 * scale, 48 * scale, 15 * scale);
            Line(g, outline, 24 * scale, body + forward * (11 * scale), body - forward * (11 * scale));
            Line(g, outline, 15 * scale, neckBase, head);
            Line(g, outline, 17 * scale, head, head1);
            Line(g, outline, 12 * scale, head1, head2);
            Line(g, friend ? Color.FromArgb(184, 178, 158) : Color.FromArgb(232, 233, 228), 15 * scale, under + forward * (7 * scale), under - forward * (7 * scale));
            Line(g, white, 22 * scale, body + forward * (11 * scale), body - forward * (11 * scale));
            Line(g, neck, 13 * scale, neckBase, head);
            Line(g, neck, 15 * scale, head, head1);
            Line(g, neck, 10 * scale, head1, head2);
            if (friend)
            {
                Line(g, Color.FromArgb(168, 158, 134), 2 * scale, body - forward * (10 * scale) + new V(0, -2 * scale), body + forward * (3 * scale) + new V(0, 2 * scale));
                Line(g, Color.FromArgb(244, 240, 220), 5 * scale, head + new V(0, 3 * scale) - side * (4 * scale), head + new V(0, 3 * scale) + side * (4 * scale));
            }
            Line(g, orange, 9 * scale, head2, head2 + headForward * (3 * scale));
            V eyeSide = new V(side.X * 1.3f, side.Y * .4f) * (5 * scale), eyeCenter = head + headForward * (5 * scale) + new V(0, -3 * scale);
            if (friend && !asleep)
            {
                Circle(g, Color.FromArgb(181, 177, 151), eyeCenter - eyeSide, 2.2f * scale);
                Circle(g, Color.FromArgb(181, 177, 151), eyeCenter + eyeSide, 2.2f * scale);
            }
            if (asleep)
            {
                Line(g, Color.Black, scale, eyeCenter - eyeSide - new V(scale, 0), eyeCenter - eyeSide + new V(scale, 0));
                Line(g, Color.Black, scale, eyeCenter + eyeSide - new V(scale, 0), eyeCenter + eyeSide + new V(scale, 0));
            }
            else
            {
                Circle(g, Color.Black, eyeCenter - eyeSide, 1.6f * scale);
                Circle(g, Color.Black, eyeCenter + eyeSide, 1.6f * scale);
            }
            if (hat == "Scarf")
            {
                V scarf = neckBase + (head - neckBase) * .35f;
                Line(g, Color.FromArgb(195, 66, 43), 5 * scale, scarf - new V(5 * scale, 0), scarf + new V(5 * scale, 0));
                Line(g, Color.FromArgb(195, 66, 43), 4 * scale, scarf, scarf + new V(-7 * scale, 9 * scale));
            }
            if (hat == "Sunhat")
            {
                using (var b = new SolidBrush(Color.FromArgb(227, 193, 126)))
                {
                    g.FillEllipse(b, head.X - 14 * scale, head.Y - 13 * scale, 28 * scale, 6 * scale);
                    g.FillRectangle(b, head.X - 7 * scale, head.Y - 22 * scale, 14 * scale, 12 * scale);
                }
                Line(g, Color.FromArgb(163, 93, 60), 2 * scale, head + new V(-6 * scale, -12 * scale), head + new V(6 * scale, -12 * scale));
            }
            if (asleep)
                using (var brush = new SolidBrush(Color.FromArgb(210, 110, 110, 110)))
                    g.DrawString("z z", sleepyFont, brush, pos.X + 25 * scale, pos.Y - 55 * scale);
        }
        public static void Scene(Graphics g, World world)
        {
            using (var footprints = new FootprintRenderer())
                Scene(g, world, footprints);
        }
        public static void Scene(Graphics g, World world, FootprintRenderer footprints)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            foreach (var o in world.Objects)
                if (o.Surface)
                    Object(g, o, world.Scale, world.Time);
            footprints.Draw(g, world);
            if (world.Exploding)
            {
                Burst(g, world, world.Goose, false);
                if (world.Data.Companion)
                    Burst(g, world, world.Friend, true);
                return;
            }
            if (world.Data.Companion && world.Friend.Pos.Y < world.Goose.Pos.Y)
                Goose(g, world.Friend, world.GooseScale(world.Friend), world.Data.Hat, true, world.Data.ReducedMotion);
            Goose(g, world.Goose, world.Scale, world.Data.Hat, false, world.Data.ReducedMotion);
            if (world.Data.Companion && world.Friend.Pos.Y >= world.Goose.Pos.Y)
                Goose(g, world.Friend, world.GooseScale(world.Friend), world.Data.Hat, true, world.Data.ReducedMotion);
            foreach (var goose in world.Data.Companion ? new[] { world.Goose, world.Friend } : new[] { world.Goose })
                if (goose.Swimming)
                {
                    using (var pen = new Pen(Color.FromArgb(155, 217, 245, 250), 2))
                        for (int i = 0; i < 2; i++)
                        {
                            float r = 24 + (world.Data.ReducedMotion ? i * 13 : (world.Time * 18 + i * 13) % 26);
                            g.DrawEllipse(pen, goose.Pos.X - r, goose.Pos.Y - 6, r * 2, 12);
                        }
                }
            if (world.CarryingStick)
                Stick(g, world.Beak(world.Goose), world.Scale);
            if (world.DismissRemaining > 0)
            {
                V p = DismissPosition(world);
                using (var b = new SolidBrush(Color.FromArgb(225, 35, 43, 39)))
                    g.FillEllipse(b, p.X, p.Y, 72, 36);
                using (var f = new Font("Segoe UI", 10, FontStyle.Bold))
                    g.DrawString("Bye in " + Math.Ceiling(world.DismissRemaining), f, Brushes.White, p.X + 7, p.Y + 8);
            }
        }
        public static void Object(Graphics g, DesktopObject o, float scale, float time)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            V p = o.Pos;
            float r = o.Size.X / 2;
            if (o.Surface)
            {
                Color c = o.Kind == ToyKind.Mud ? Color.FromArgb(195, 126, 91, 57) : o.Kind == ToyKind.Pond ? Color.FromArgb(195, 78, 162, 190) : Color.FromArgb(115, 105, 180, 203);
                using (var b = new SolidBrush(c))
                    g.FillEllipse(b, p.X - o.Size.X / 2, p.Y - o.Size.Y / 2, o.Size.X, o.Size.Y);
                using (var pen = new Pen(o.Kind == ToyKind.Mud ? Color.FromArgb(150, 88, 65, 46) : Color.FromArgb(180, 192, 235, 241), 3))
                    g.DrawArc(pen, p.X - o.Size.X * .43f, p.Y - o.Size.Y * .38f, o.Size.X * .86f, o.Size.Y * .76f, 190, 110);
                return;
            }
            if (o.Kind == ToyKind.Stick)
            {
                Stick(g, p, Math.Max(1, o.Size.X / 28));
                return;
            }
            if (o.Kind == ToyKind.Peas)
            {
                for (int i = 0; i < 6; i++)
                {
                    V at = p + new V((i % 3 - 1) * 9, (i / 3 - .5f) * 9);
                    Circle(g, Color.FromArgb(105, 164, 64), at, 4);
                    Circle(g, Color.FromArgb(183, 215, 113), at + new V(-1, -1), 1);
                }
                return;
            }
            if (o.Kind == ToyKind.Duck)
            {
                using (var b = new SolidBrush(Color.FromArgb(251, 205, 55)))
                    g.FillEllipse(b, p.X - 15, p.Y - 1, 30, 18);
                Circle(g, Color.FromArgb(255, 220, 71), p + new V(7, -5), 9);
                Line(g, Color.FromArgb(239, 132, 41), 5, p + new V(14, -4), p + new V(21, -4));
                Circle(g, Color.FromArgb(60, 49, 28), p + new V(10, -8), 1.5f);
                return;
            }
            Circle(g, Color.FromArgb(232, 90, 64), p, r);
            using (var pen = new Pen(Color.FromArgb(249, 228, 174), 3))
                g.DrawArc(pen, p.X - r + 3, p.Y - r + 3, r * 2 - 6, r * 2 - 6, time * 80, 140);
            Circle(g, Color.FromArgb(100, 255, 255, 255), p + new V(-r * .3f, -r * .3f), r * .25f);
        }
        public static void Interactions(Graphics g, World world)
        {
            if (world.Exploding)
                return;
            foreach (var o in world.Objects)
            {
                if (!o.Surface && !o.Carried)
                    Object(g, o, world.Scale, world.Time);
                if (world.Editing)
                {
                    V corner = o.Pos + new V(o.Size.X / 2, -o.Size.Y / 2);
                    Circle(g, Color.FromArgb(230, 64, 73, 65), corner, 9);
                    Line(g, Color.White, 1.5f, corner - new V(3, 3), corner + new V(3, 3));
                    Line(g, Color.White, 1.5f, corner - new V(3, -3), corner + new V(3, -3));
                    if (o.Surface)
                    {
                        V move = o.Pos - new V(0, o.Size.Y / 2), resize = o.Pos + o.Size / 2;
                        Circle(g, Color.FromArgb(235, 255, 255, 244), move, 10);
                        Line(g, Color.FromArgb(80, 104, 89), 2, move - new V(5, 0), move + new V(5, 0));
                        Circle(g, Color.FromArgb(235, 255, 255, 244), resize, 9);
                        Line(g, Color.FromArgb(80, 104, 89), 2, resize - new V(3, -3), resize + new V(3, -3));
                    }
                }
            }
        }
        static void Burst(Graphics g, World w, Goose goose, bool friend)
        {
            float t = w.ExplosionTime, duration = w.Data.ReducedMotion ? .25f : .8f, alpha = Math.Max(0, 1 - t / duration);
            if (w.Data.ReducedMotion)
            {
                using (var bitmap = new Bitmap(200, 200))
                {
                    using (var bg = Graphics.FromImage(bitmap))
                    {
                        bg.TranslateTransform(100 - goose.Pos.X, 140 - goose.Pos.Y);
                        Goose(bg, goose, w.GooseScale(goose), w.Data.Hat, friend, true);
                    }
                    using (var attributes = new System.Drawing.Imaging.ImageAttributes())
                    {
                        var matrix = new System.Drawing.Imaging.ColorMatrix();
                        matrix.Matrix33 = alpha;
                        attributes.SetColorMatrix(matrix);
                        g.DrawImage(bitmap, new Rectangle((int)goose.Pos.X - 100, (int)goose.Pos.Y - 140, 200, 200), 0, 0, 200, 200, GraphicsUnit.Pixel, attributes);
                    }
                }
                return;
            }
            V origin = goose.Pos + new V(0, -25);
            for (int i = 0; i < 7; i++)
            {
                V p = origin + V.Direction(i * .897f) * (t * 65);
                Circle(g, Color.FromArgb((int)(alpha * 130), 213, 218, 214), p, 12 + t * 22);
            }
            for (int i = 0; i < 22; i++)
            {
                float a = i * 2.399f;
                V p = origin + V.Direction(a) * (t * (75 + i % 5 * 35)) + new V(0, t * t * 85);
                var state = g.Save();
                g.TranslateTransform(p.X, p.Y);
                g.RotateTransform(a * 57 + t * 160);
                using (var b = new SolidBrush(Color.FromArgb((int)(alpha * 255), 255, 255, 248)))
                    g.FillEllipse(b, -8, -3, 16, 6);
                g.Restore(state);
            }
        }
        static void Stick(Graphics g, V p, float scale)
        {
            Line(g, Color.FromArgb(128, 92, 54), 3 * scale, p - new V(13 * scale, -4 * scale), p + new V(13 * scale, -4 * scale));
            Line(g, Color.FromArgb(128, 92, 54), 2 * scale, p + new V(4 * scale, 0), p + new V(6 * scale, -8 * scale));
        }
    }
}
