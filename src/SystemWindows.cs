using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace SecondHonk
{
    // Windows-side window source for the broker (Card 4): discovers eligible
    // top-level windows on the selected display and applies SetWindowPos
    // moves that preserve size, activation and Z-order. Never touches
    // goose-owned windows, other processes are read with plain queries, and
    // every hook is released when this is disposed.
    public sealed class SystemWindowAdapter : IWindowSystem, IDisposable
    {
        const int SWP_NOSIZE = 0x0001, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010;
        const uint EVENT_SYSTEM_MOVESIZESTART = 0x000A, EVENT_SYSTEM_MOVESIZEEND = 0x000B;
        const uint WINEVENT_OUTOFCONTEXT = 0, WINEVENT_SKIPOWNPROCESS = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWPLACEMENT
        {
            public int Length, Flags, ShowCmd;
            public Point MinPosition, MaxPosition;
            public RECT NormalPosition;
        }

        delegate void WinEventDelegate(IntPtr hook, uint idEvent, IntPtr hwnd, int idObject, int idChild, uint idThread, uint time);
        delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern bool IsWindow(IntPtr hwnd);
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hwnd);
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")]
        static extern bool GetWindowPlacement(IntPtr hwnd, ref WINDOWPLACEMENT placement);
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int max);
        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventDelegate callback, uint processId, uint threadId, uint flags);
        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hook);

        readonly Rectangle display;
        readonly Func<IntPtr, bool> isOwned;
        readonly uint ownProcessId;
        readonly WinEventDelegate hookProc;
        readonly IntPtr hook;
        readonly object gate = new object();
        readonly Queue<IntPtr> disturbed = new Queue<IntPtr>();
        readonly HashSet<IntPtr> userHeld = new HashSet<IntPtr>();
        bool disposed;

        // Raised on the UI thread by Poll when the user grabs, moves or
        // resizes a window. The session forwards ids to the broker.
        public event Action<string> UserIntervened;

        public SystemWindowAdapter(Rectangle selectedDisplay, Func<IntPtr, bool> ownedWindows)
        {
            display = selectedDisplay;
            isOwned = ownedWindows ?? (h => false);
            ownProcessId = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
            hookProc = OnWinEvent;
            try
            {
                hook = SetWinEventHook(EVENT_SYSTEM_MOVESIZESTART, EVENT_SYSTEM_MOVESIZEEND, IntPtr.Zero, hookProc, 0, 0, WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS);
            }
            catch (Exception error)
            {
                Program.Log(error);
                hook = IntPtr.Zero;
            }
        }

        public V DisplaySize
        {
            get
            {
                return new V(display.Width, display.Height);
            }
        }

        public IWindowTarget ActiveWindow()
        {
            try
            {
                IntPtr foreground = GetForegroundWindow();
                if (foreground == IntPtr.Zero || isOwned(foreground) || OwnProcess(foreground))
                    return null;
                return new NativeTarget(this, foreground);
            }
            catch (Exception error)
            {
                Program.Log(error);
                return null;
            }
        }

        public IEnumerable<IWindowTarget> InactiveWindows()
        {
            var found = new List<IWindowTarget>();
            try
            {
                IntPtr foreground = GetForegroundWindow();
                EnumWindows((hwnd, param) =>
                {
                    if (found.Count >= 32 || hwnd == foreground || isOwned(hwnd) || OwnProcess(hwnd))
                        return true;
                    var target = new NativeTarget(this, hwnd);
                    if (target.IsEligible)
                        found.Add(target);
                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception error)
            {
                Program.Log(error);
            }

            return found;
        }

        // Drains native move/size notifications on the UI thread. Hook
        // callbacks only queue; this raises the events deterministically.
        public void Poll()
        {
            List<IntPtr> pending = null;
            lock (gate)
            {
                while (disturbed.Count > 0)
                {
                    if (pending == null)
                        pending = new List<IntPtr>();
                    pending.Add(disturbed.Dequeue());
                }
            }

            if (pending == null)
                return;
            var handler = UserIntervened;
            if (handler == null)
                return;
            foreach (IntPtr hwnd in pending)
                try
                {
                    handler(hwnd.ToInt64().ToString());
                }
                catch (Exception error)
                {
                    Program.Log(error);
                }
        }

        void OnWinEvent(IntPtr hookHandle, uint idEvent, IntPtr hwnd, int idObject, int idChild, uint idThread, uint time)
        {
            try
            {
                if (hwnd == IntPtr.Zero || idObject != 0 || idChild != 0)
                    return;
                lock (gate)
                {
                    if (idEvent == EVENT_SYSTEM_MOVESIZESTART)
                    {
                        userHeld.Add(hwnd);
                        disturbed.Enqueue(hwnd);
                    }
                    else if (idEvent == EVENT_SYSTEM_MOVESIZEEND)
                    {
                        userHeld.Remove(hwnd);
                    }
                }
            }
            catch (Exception error)
            {
                Program.Log(error);
            }
        }

        bool OwnProcess(IntPtr hwnd)
        {
            uint pid;
            return GetWindowThreadProcessId(hwnd, out pid) != 0 && pid == ownProcessId;
        }

        bool HeldByUser(IntPtr hwnd)
        {
            lock (gate)
                return userHeld.Contains(hwnd);
        }

        static string TitleOf(IntPtr hwnd)
        {
            var text = new StringBuilder(256);
            try
            {
                return GetWindowText(hwnd, text, text.Capacity) > 0 ? text.ToString() : "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (hook != IntPtr.Zero)
                try
                {
                    UnhookWinEvent(hook);
                }
                catch (Exception error)
                {
                    Program.Log(error);
                }
        }

        sealed class NativeTarget : IWindowTarget
        {
            readonly SystemWindowAdapter adapter;
            readonly IntPtr handle;
            readonly string title;

            public NativeTarget(SystemWindowAdapter owner, IntPtr window)
            {
                adapter = owner;
                handle = window;
                title = TitleOf(window);
            }

            public string Id
            {
                get
                {
                    return handle.ToInt64().ToString();
                }
            }

            public bool IsValid
            {
                get
                {
                    try
                    {
                        return IsWindow(handle) && IsWindowVisible(handle) && TitleOf(handle) == title;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

            public bool IsEligible
            {
                get
                {
                    try
                    {
                        if (!IsWindowVisible(handle) || IsIconic(handle))
                            return false;
                        var placement = new WINDOWPLACEMENT();
                        placement.Length = Marshal.SizeOf(typeof(WINDOWPLACEMENT));
                        // Minimized, maximized and fullscreen windows are
                        // never prank targets.
                        if (GetWindowPlacement(handle, ref placement) && (placement.ShowCmd == 2 || placement.ShowCmd == 3))
                            return false;
                        RECT rect;
                        if (!GetWindowRect(handle, out rect))
                            return false;
                        var bounds = new Rectangle(adapter.display.X, adapter.display.Y, adapter.display.Width, adapter.display.Height);
                        var window = new Rectangle(rect.Left, rect.Top, Math.Max(0, rect.Right - rect.Left), Math.Max(0, rect.Bottom - rect.Top));
                        if (window.Width < 80 || window.Height < 40 || !window.IntersectsWith(bounds))
                            return false;
                        if (window.Width >= bounds.Width && window.Height >= bounds.Height)
                            return false;
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

            public bool IsForeground
            {
                get
                {
                    try
                    {
                        return GetForegroundWindow() == handle;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

            public bool UserHeld
            {
                get
                {
                    return adapter.HeldByUser(handle);
                }
            }

            public V Position
            {
                get
                {
                    try
                    {
                        RECT rect;
                        if (!GetWindowRect(handle, out rect))
                            return new V();
                        return new V(rect.Left - adapter.display.X, rect.Top - adapter.display.Y);
                    }
                    catch (Exception)
                    {
                        return new V();
                    }
                }
            }

            public V Size
            {
                get
                {
                    try
                    {
                        RECT rect;
                        if (!GetWindowRect(handle, out rect))
                            return new V(80, 40);
                        return new V(Math.Max(1, rect.Right - rect.Left), Math.Max(1, rect.Bottom - rect.Top));
                    }
                    catch (Exception)
                    {
                        return new V(80, 40);
                    }
                }
            }

            public bool MoveTo(V position, WindowMoveFlags flags)
            {
                try
                {
                    if (!IsValid)
                        return false;
                    int x = adapter.display.X + (int)position.X, y = adapter.display.Y + (int)position.Y;
                    uint native = SWP_NOZORDER | SWP_NOACTIVATE;
                    if ((flags & WindowMoveFlags.PreserveSize) != 0)
                        native |= SWP_NOSIZE;
                    return SetWindowPos(handle, IntPtr.Zero, x, y, 0, 0, native);
                }
                catch (Exception error)
                {
                    Program.Log(error);
                    return false;
                }
            }
        }
    }
}
