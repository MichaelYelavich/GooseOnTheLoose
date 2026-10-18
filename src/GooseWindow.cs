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
    public sealed class GooseWindow : Form
    {
        PreparedPicture prepared;
        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }
        public GooseWindow(string note, string imagePath) : this(note, ReadPicture(imagePath)) { }
        static PreparedPicture ReadPicture(string path)
        {
            if (String.IsNullOrEmpty(path))
                return null;
            try
            {
                return new PreparedPicture(Pictures.ReadValidated(path, System.Threading.CancellationToken.None));
            }
            catch (Exception error) { Program.Log(error); return null; }
        }
        internal GooseWindow(string note, PreparedPicture picture)
        {
            prepared = picture;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = true;
            TopMost = true;
            BackColor = Color.White;
            MinimumSize = new Size(220, 150);
            ClientSize = new Size(350, 210);
            Text = "Goose Notepad";
            if (prepared != null)
            {
                Text = "A gift from your goose";
                ClientSize = new Size(360, 280);
                Controls.Add(new PictureBox { Image = prepared.Image, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill, BackColor = Color.White });
            }
            if (prepared == null)
                Controls.Add(new TextBox { Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, Dock = DockStyle.Fill, Text = note.Replace("\r\n", "\n").Replace("\n", "\r\n"), Font = new Font("Consolas", 12), BackColor = Color.White, ForeColor = Color.Black, ScrollBars = ScrollBars.Vertical });
        }
        protected override void Dispose(bool disposing)
        {
            try
            {
                base.Dispose(disposing);
            }
            finally { if (disposing && prepared != null) { prepared.Dispose(); prepared = null; } }
        }
    }
}
