using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.ComponentModel;
using System.Runtime.InteropServices;
namespace SecondHonk
{
    // A top-down premultiplied DIB is shared by GDI+ and UpdateLayeredWindow.
    // It is owned by exactly one overlay and recreated only when its size changes.
    sealed class LayeredBuffer : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        struct Header
        {
            public uint Size; public int Width, Height; public ushort Planes, BitCount;
            public uint Compression, SizeImage; public int XPels, YPels; public uint Used, Important;
        }
        [DllImport("gdi32.dll", SetLastError = true)] static extern IntPtr CreateDIBSection(IntPtr dc, ref Header info, uint usage, out IntPtr pixels, IntPtr section, uint offset);
        IntPtr dc, bitmap, previous;
        public Bitmap Frame
        {
            get; private set;
        }
        public LayeredBuffer(int width, int height)
        {
            try
            {
                dc = Native.CreateCompatibleDC(IntPtr.Zero);
                if (dc == IntPtr.Zero)
                    throw new Win32Exception();
                var info = new Header { Size = (uint)Marshal.SizeOf(typeof(Header)), Width = width, Height = -height, Planes = 1, BitCount = 32 };
                IntPtr pixels;
                bitmap = CreateDIBSection(dc, ref info, 0, out pixels, IntPtr.Zero, 0);
                if (bitmap == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                previous = Native.SelectObject(dc, bitmap);
                if (previous == IntPtr.Zero || previous == new IntPtr(-1))
                    throw new Win32Exception();
                Frame = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, pixels);
            }
            catch { Dispose(); throw; }
        }
        public void Present(IntPtr handle, Point position)
        {
            Point origin = Point.Empty;
            Size size = Frame.Size;
            var blend = new Native.Blend { Alpha = 255, Format = 1 };
            if (!Native.UpdateLayeredWindow(handle, IntPtr.Zero, ref position, ref size, dc, ref origin, 0, ref blend, 2))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        public void Dispose()
        {
            if (Frame != null)
            {
                Frame.Dispose();
                Frame = null;
            }
            if (previous != IntPtr.Zero && previous != new IntPtr(-1) && dc != IntPtr.Zero)
                Native.SelectObject(dc, previous);
            previous = IntPtr.Zero;
            if (bitmap != IntPtr.Zero)
            {
                Native.DeleteObject(bitmap);
                bitmap = IntPtr.Zero;
            }
            if (dc != IntPtr.Zero)
            {
                Native.DeleteDC(dc);
                dc = IntPtr.Zero;
            }
        }
    }
}
