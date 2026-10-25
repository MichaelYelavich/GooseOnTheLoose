using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecondHonk
{
    // Native wardrobe tool (Card 4): previews of both geese plus per-slot
    // controls. Per-goose choices are explicit individual outfits; the link
    // checkbox returns to matching outfits, applying the selected goose's
    // outfit to both. Every change saves and renders live.
    public sealed class WardrobeWindow : Form
    {
        readonly World world;
        readonly ComboBox[] heads = new ComboBox[2], necks = new ComboBox[2], glasses = new ComboBox[2];
        readonly PictureBox[] previews = new PictureBox[2];
        readonly CheckBox link;
        readonly RadioButton sourceWhite, sourceCanada;
        bool refreshing;

        public WardrobeWindow(World w)
        {
            world = w;
            Text = "Goose Wardrobe";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(460, 400);
            BackColor = Color.FromArgb(249, 247, 239);
            Font = new Font("Segoe UI", 9);
            var columns = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 300,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10)
            };
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            string[] names = { "White goose", "Canada goose" };
            for (int goose = 0; goose < 2; goose++)
            {
                int pick = goose;
                var group = new GroupBox { Text = names[goose], Dock = DockStyle.Fill, Padding = new Padding(8) };
                var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
                var preview = new PictureBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(229, 233, 224), BorderStyle = BorderStyle.FixedSingle };
                preview.Paint += (s, e) => PaintPreview(e.Graphics, preview, pick);
                previews[goose] = preview;
                layout.Controls.Add(preview);
                heads[goose] = SlotRow(layout, "Head", OutfitCatalog.Heads, value =>
                {
                    world.Data.SetOutfitSlot(pick, OutfitSlot.Head, value);
                    Saved();
                });
                necks[goose] = SlotRow(layout, "Neck", OutfitCatalog.Necks, value =>
                {
                    world.Data.SetOutfitSlot(pick, OutfitSlot.Neck, value);
                    Saved();
                });
                glasses[goose] = SlotRow(layout, "Glasses", OutfitCatalog.Glasses, value =>
                {
                    world.Data.SetOutfitSlot(pick, OutfitSlot.Glasses, value);
                    Saved();
                });
                group.Controls.Add(layout);
                columns.Controls.Add(group);
            }

            Controls.Add(columns);
            var bottom = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 100, ColumnCount = 1, RowCount = 3, Padding = new Padding(10, 0, 10, 10) };
            link = new CheckBox { Text = "Link outfits (dress alike)", Dock = DockStyle.Fill };
            link.CheckedChanged += (s, e) =>
            {
                if (refreshing)
                    return;
                world.Data.SetOutfitsLinked(link.Checked, sourceWhite.Checked ? 0 : 1);
                Saved();
            };
            bottom.Controls.Add(link);
            var source = new FlowLayoutPanel { Dock = DockStyle.Fill };
            source.Controls.Add(new Label { Text = "When linking, apply", AutoSize = true, Padding = new Padding(0, 4, 0, 0) });
            sourceWhite = new RadioButton { Text = "White's", Checked = true, AutoSize = true };
            sourceCanada = new RadioButton { Text = "Canada's", AutoSize = true };
            sourceWhite.CheckedChanged += (s, e) =>
            {
                if (refreshing || !sourceWhite.Checked || link.Checked)
                    return;
                world.Data.SetOutfitsLinked(true, 0);
                Saved();
            };
            sourceCanada.CheckedChanged += (s, e) =>
            {
                if (refreshing || !sourceCanada.Checked || link.Checked)
                    return;
                world.Data.SetOutfitsLinked(true, 1);
                Saved();
            };
            source.Controls.Add(sourceWhite);
            source.Controls.Add(sourceCanada);
            source.Controls.Add(new Label { Text = "outfit to both.", AutoSize = true, Padding = new Padding(0, 4, 0, 0) });
            bottom.Controls.Add(source);
            bottom.Controls.Add(new Label { Text = "Linked changes dress both geese, except slots set for one goose above.", Dock = DockStyle.Fill });
            Controls.Add(bottom);
            RefreshAll();
        }

        static ComboBox SlotRow(TableLayoutPanel layout, string title, string[] values, Action<string> apply)
        {
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, Height = 26 };
            row.Controls.Add(new Label { Text = title, Width = 52, Padding = new Padding(0, 4, 0, 0) });
            var box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120 };
            box.Items.AddRange(values);
            box.SelectedIndexChanged += (s, e) =>
            {
                if (box.SelectedItem != null)
                    apply(box.SelectedItem.ToString());
            };
            row.Controls.Add(box);
            layout.Controls.Add(row);
            return box;
        }

        void PaintPreview(Graphics g, PictureBox box, int goose)
        {
            var staged = new Goose(new V(box.Width / 2, box.Height - 28), goose, goose == 0 ? "White" : "Canada");
            staged.Direction = staged.HeadDirection = -.3f;
            staged.FeetReady = true;
            staged.LeftFoot = staged.RightFoot = staged.Pos;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Art.Goose(g, staged, 1.5f, world.Data.GetOutfit(goose), goose == 1, false);
        }

        void Saved()
        {
            Store.Save(world.Data);
            RefreshAll();
        }

        void RefreshAll()
        {
            refreshing = true;
            try
            {
                for (int goose = 0; goose < 2; goose++)
                {
                    heads[goose].SelectedItem = world.Data.GetOutfitSlot(goose, OutfitSlot.Head);
                    necks[goose].SelectedItem = world.Data.GetOutfitSlot(goose, OutfitSlot.Neck);
                    glasses[goose].SelectedItem = world.Data.GetOutfitSlot(goose, OutfitSlot.Glasses);
                    previews[goose].Invalidate();
                }

                link.Checked = world.Data.LinkedOutfits;
            }
            finally
            {
                refreshing = false;
            }
        }
    }
}
