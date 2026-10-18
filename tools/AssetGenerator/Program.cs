using System;
using System.Drawing;
using System.IO;
namespace SecondHonk
{
    static class AssetGenerator
    {
        [STAThread]
        static void Main(string[] args)
        {
            MakeAssets(args[0]);
        }
        public static void MakeAssets(string root)
        {
            using (var icon = Native.MakeIcon())
            using (var stream = File.Create(Path.Combine(root, "GooseOnTheLoose.ico")))
                icon.Save(stream);
            string folder = Path.Combine(root, "Packs", "Images");
            Directory.CreateDirectory(folder);
            using (var bitmap = new Bitmap(480, 320))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.FromArgb(245, 245, 238));
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var font = new Font("Segoe UI", 34, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString("THIS COULD HAVE\nBEEN A HONK.", font, Brushes.Black, new RectangleF(24, 50, 432, 220), format);
                bitmap.Save(Path.Combine(folder, "honk.png"), System.Drawing.Imaging.ImageFormat.Png);
            }
        }
    }
}
