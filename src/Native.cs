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
}
