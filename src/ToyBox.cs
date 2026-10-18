using System;
using System.Drawing;
using System.Windows.Forms;
namespace SecondHonk
{
    public sealed class ToyBox : Form
    {
        readonly World world; readonly Func<Rectangle> area; readonly Label hint = new Label { Dock = DockStyle.Bottom, Height = 56, TextAlign = ContentAlignment.MiddleCenter, Text = "Drag toys onto the desktop.\r\nMove water with its handles." };
        public ToyBox(World w, Func<Rectangle> display)
        {
            world = w;
            area = display;
            Text = "Toy Box";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(330, 360);
            BackColor = Color.FromArgb(249, 247, 239);
            Font = new Font("Segoe UI", 9);
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3, Padding = new Padding(10) };
            for (int i = 0; i < 3; i++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33f));
            }
            string[] names = { "Ball", "Fetch stick", "Peas", "Rubber duck", "Pond", "Puddle", "Mud patch" };
            int n = 0;
            foreach (ToyKind kind in Enum.GetValues(typeof(ToyKind)))
            {
                ToyKind selected = kind;
                var bitmap = new Bitmap(58, 40);
                using (var g = Graphics.FromImage(bitmap))
                {
                    var o = new DesktopObject { Kind = kind, Pos = new V(29, 22), Size = kind == ToyKind.Pond || kind == ToyKind.Puddle || kind == ToyKind.Mud ? new V(48, 25) : new V(28, 28) };
                    Art.Object(g, o, 1, 0);
                }
                var button = new Button { Text = names[n++], Image = bitmap, TextImageRelation = TextImageRelation.ImageAboveText, ImageAlign = ContentAlignment.MiddleCenter, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, BackColor = Color.White, Margin = new Padding(3), AccessibleName = names[n - 1] };
                button.FlatAppearance.BorderColor = Color.FromArgb(222, 221, 210);
                button.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { button.Capture = true; hint.Text = "Release on the selected display to place it."; } };
                button.MouseUp += (s, e) => { button.Capture = false; Rectangle bounds = area(); Point p = Cursor.Position; if (bounds.Contains(p) && !Bounds.Contains(p)) { var o = world.PlaceObject(selected, new V(p.X - bounds.X, p.Y - bounds.Y)); hint.Text = o == null ? "Toy box is full (16 objects). Clear or remove one." : "Drag another toy out. Water handles are ready."; } else hint.Text = "Drag the toy out onto the selected display."; };
                grid.Controls.Add(button);
                button.Disposed += (s, e) => bitmap.Dispose();
            }
            var clear = new Button { Text = "Clear desktop", Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat };
            clear.Click += (s, e) => world.ClearProps();
            grid.Controls.Add(clear);
            Controls.Add(grid);
            Controls.Add(hint);
            Shown += (s, e) => world.Editing = true;
            FormClosed += (s, e) => world.Editing = false;
        }
    }
}
