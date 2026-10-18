using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace SecondHonk
{
    // No main Form: this context owns only the goose overlay, tray and goose-created notes.
    public sealed class DesktopSession : ApplicationContext
    {
        public readonly World World;
        readonly NotifyIcon tray = new NotifyIcon();
        readonly ContextMenuStrip menu = new ContextMenuStrip();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        readonly Stopwatch watch = Stopwatch.StartNew();
        readonly HonkAudio audio = new HonkAudio();
        readonly ToolDialogOwner dialogOwner = new ToolDialogOwner();
        int modalDepth;
        readonly CommandWindow commands;
        readonly List<GooseWindow> windows = new List<GooseWindow>();
        readonly Dictionary<string, EventWaitHandle> signals = new Dictionary<string, EventWaitHandle>();
        DesktopOverlay overlay;
        DesktopOverlay toys;
        ToyBox toolbox;
        NoteEditor editor;
        readonly ContentMonitor contentMonitor;
        readonly ShutdownClock shutdown = new ShutdownClock();
        GooseWindow carried;
        readonly PicturePreparation pictures = new PicturePreparation();
        CancellationTokenSource deliveryCancel;
        int deliveryRevision;
        bool paintedPause;
        bool exiting, suppressRetaliation, leftWasDown;
        double lastTime;
        float quietMenuIn;
        readonly bool testing;
        public DesktopSession(SaveData settings, Content content, string initialCommand) : this(settings, content, initialCommand, false)
        {
        }

        internal DesktopSession(SaveData settings, Content content, string initialCommand, bool test)
        {
            testing = test;
            World = new World(settings, content, Environment.TickCount);
            contentMonitor = new ContentMonitor(World);
            commands = new CommandWindow(HandleCommand, testing);
            BuildMenu();
            tray.Icon = Native.MakeIcon();
            tray.ContextMenuStrip = menu;
            tray.Text = "Goose on the Loose";
            tray.Visible = !testing;
            tray.DoubleClick += (s, e) => World.Call(Pointer());
            World.Honked += audio.Play;
            World.FootLanded += () =>
            {
                if (World.Goose.Task == Activity.Mud)
                    audio.Mud();
                else
                    audio.Step();
            };
            World.CursorCaught += audio.Bite;
            World.DeliveryStarted += BeginDelivery;
            World.DeliveryCompleted += () => carried = null;
            ChooseMonitor(settings.Monitor);
            World.Goose.Place(World.Clamp(new V(World.Width * .45f, World.Height * .68f)));
            World.Friend.Place(World.Clamp(World.Goose.Pos + new V(-80, 20)));
            World.Pointer = Pointer();
            World.Update(0);
            overlay.RenderFrame();
            contentMonitor.SettingsChanged += () =>
            {
                paintedPause = false;
                Screen selected = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == World.Data.Monitor) ?? Screen.PrimaryScreen;
                if (!testing && selected.WorkingArea != overlay.Area)
                    ChooseMonitor(World.Data.Monitor);
                World.Goose.FeetReady = World.Friend.FeetReady = false;
                World.GrabRemaining = 0;
            };
            timer.Interval = 33;
            timer.Tick += Tick;
            timer.Start();
            lastTime = watch.Elapsed.TotalSeconds;
            string prefix = testing ? CommandRegistry.Prefix + "SelfTest." + Guid.NewGuid().ToString("N") + "." : CommandRegistry.Prefix;
            foreach (var command in CommandRegistry.All)
                signals[command.Name] = new EventWaitHandle(false, EventResetMode.AutoReset, prefix + command.Name);
            Microsoft.Win32.SystemEvents.SessionEnding += SessionEnding;
            if (initialCommand.Length > 0 && signals.ContainsKey(initialCommand))
                signals[initialCommand].Set();
            WriteStatus();
        }

        V Pointer()
        {
            Point p = Cursor.Position;
            return new V(p.X - overlay.Area.X, p.Y - overlay.Area.Y);
        }

        ToolStripMenuItem Item(ToolStripItemCollection list, string title, Action action)
        {
            var item = new ToolStripMenuItem(title);
            item.Click += (s, e) =>
            {
                action();
                paintedPause = false;
            };
            list.Add(item);
            return item;
        }

        void Toggle(ToolStripItemCollection list, string title, Func<bool> get, Action<bool> set)
        {
            var item = new ToolStripMenuItem(title)
            {
                CheckOnClick = false
            };
            item.Click += (s, e) =>
            {
                paintedPause = false;
                set(!get());
                item.Checked = get();
                Store.Save(World.Data);
            };
            menu.Opening += (s, e) => item.Checked = get();
            list.Add(item);
        }

        void BuildMenu()
        {
            Item(menu.Items, "Toy Box", ShowToolbox);
            menu.Items.Add(new ToolStripSeparator());
            Item(menu.Items, "Honk                         Ctrl+Alt+H", World.Honk);
            Item(menu.Items, "Come here                  Ctrl+Alt+G", () => World.Call(Pointer()));
            var play = new ToolStripMenuItem("Play");
            menu.Items.Add(play);
            Item(play.DropDownItems, "Drop peas at cursor       Ctrl+Alt+F", () => World.Feed(Pointer()));
            Item(play.DropDownItems, "Throw a ball                  Ctrl+Alt+B", () => World.ThrowBall(Pointer()));
            Item(play.DropDownItems, "Throw a stick                 Ctrl+Alt+T", () => World.Fetch(Pointer()));
            Item(play.DropDownItems, "Follow the cursor", () => World.Follow(Pointer()));
            var mischief = new ToolStripMenuItem("Mischief");
            menu.Items.Add(mischief);
            Item(mischief.DropDownItems, "Bring a note                    Ctrl+Alt+N", () => World.Deliver(false));
            Item(mischief.DropDownItems, "Bring an image                Ctrl+Alt+I", () => World.Deliver(true));
            Item(mischief.DropDownItems, "Chase the cursor", () => World.Chase(Pointer()));
            Item(mischief.DropDownItems, "Track mud", World.MakeMud);
            menu.Items.Add(new ToolStripSeparator());
            Toggle(menu.Items, "Paused                        Ctrl+Alt+P", () => World.Paused, v =>
            {
                World.Paused = v;
                World.GrabRemaining = 0;
            });
            Toggle(menu.Items, "Quiet mode                   Ctrl+Alt+K", () => World.Data.Quiet, SetQuiet);
            var options = new ToolStripMenuItem("Options");
            menu.Items.Add(options);
            Toggle(options.DropDownItems, "Reduced motion", () => World.Data.ReducedMotion, v => World.Data.ReducedMotion = v);
            Toggle(options.DropDownItems, "Sound", () => World.Data.Sound, v => World.Data.Sound = v);
            Toggle(options.DropDownItems, "Automatic mischief", () => World.Data.Mischief, v => World.Data.Mischief = v);
            Toggle(options.DropDownItems, "Mouse stealing when provoked", () => World.Data.MouseSteal, v =>
            {
                World.Data.MouseSteal = v;
                if (!v)
                    World.Cancel();
            });
            Toggle(options.DropDownItems, "Random cursor attacks", () => World.Data.RandomAttacks, v => World.Data.RandomAttacks = v);
            Toggle(options.DropDownItems, "A second goose", () => World.Data.Companion, v =>
            {
                World.Data.Companion = v;
                World.Friend.Place(World.Clamp(World.Goose.Pos + new V(-70, 25)));
            });
            Item(options.DropDownItems, "Quiet for 25 minutes", () =>
            {
                SetQuiet(true);
                World.QuietRemaining = 25 * 60;
                Store.Save(World.Data);
            });
            var hats = new ToolStripMenuItem("Appearance");
            options.DropDownItems.Add(hats);
            foreach (string hat in new[]
            {
                "None",
                "Scarf",
                "Sunhat"
            }

            )
            {
                string value = hat;
                var item = Item(hats.DropDownItems, hat == "None" ? "No accessories" : hat, () =>
                {
                    World.Data.Hat = value;
                    Store.Save(World.Data);
                });
                menu.Opening += (s, e) => item.Checked = World.Data.Hat == value;
            }

            var sizes = new ToolStripMenuItem("Size");
            options.DropDownItems.Add(sizes);
            foreach (float size in new[]
            {
                1f,
                1.5f,
                2f
            }

            )
            {
                float value = size;
                var item = Item(sizes.DropDownItems, size == 1 ? "Small" : size == 1.5f ? "Normal" : "Large", () =>
                {
                    World.Data.Scale = value;
                    World.Goose.FeetReady = World.Friend.FeetReady = false;
                    Store.Save(World.Data);
                });
                menu.Opening += (s, e) => item.Checked = World.Data.Scale == value;
            }

            var monitors = new ToolStripMenuItem("Display");
            options.DropDownItems.Add(monitors);
            foreach (Screen screen in Screen.AllScreens)
            {
                Screen value = screen;
                var item = Item(monitors.DropDownItems, screen.DeviceName + (screen.Primary ? " (primary)" : ""), () =>
                {
                    ChooseMonitor(value.DeviceName);
                    World.Data.Monitor = value.DeviceName;
                    Store.Save(World.Data);
                });
                menu.Opening += (s, e) => item.Checked = (World.Data.Monitor == value.DeviceName) || (String.IsNullOrEmpty(World.Data.Monitor) && value.Primary);
            }

            var content = new ToolStripMenuItem("Your content");
            menu.Items.Add(content);
            Item(content.DropDownItems, "Import Pictures…", () =>
            {
                RunModalTool(owner => UserContent.ImportPictures(owner, () => exiting));
            });
            Item(content.DropDownItems, "Edit Notes…", () =>
            {
                if (editor == null || editor.IsDisposed)
                    editor = new NoteEditor();
                editor.Show();
                editor.Activate();
            });
            Item(content.DropDownItems, "Open Content Folder", OpenContent);
            Item(content.DropDownItems, "Import Legacy Content Folder…", () =>
            {
                RunModalTool(owner => UserContent.ImportLegacy(owner, () => exiting));
            });
            Item(content.DropDownItems, "Reload content", () => contentMonitor.Reload());
            Item(content.DropDownItems, "Copy diagnostics", CopyDiagnostics);
            menu.Items.Add(new ToolStripSeparator());
            Item(menu.Items, "Clear desktop objects", () =>
            {
                toys.ReleaseDrag();
                World.ClearProps();
            });
            Item(menu.Items, "Close goose windows", ClearNotes);
            Item(menu.Items, "Quit                            Ctrl+Alt+Q", ExitThread);
            menu.Opening += (s, e) =>
            {
                World.GrabRemaining = 0;
                tray.Text = World.Paused ? "Goose on the Loose — paused" : World.Data.Quiet ? "Goose on the Loose — quiet" : "Goose on the Loose";
            };
        }

        void SetQuiet(bool quiet)
        {
            World.SetQuiet(quiet);
            if (quiet)
                ClearNotes();
            if (carried != null && !carried.IsDisposed)
            {
                suppressRetaliation = true;
                carried.Close();
                carried = null;
                suppressRetaliation = false;
            }
        }

        internal void ShowToolbox()
        {
            if (toolbox == null || toolbox.IsDisposed)
            {
                toolbox = new ToyBox(World, () => overlay.Area);
                toolbox.FormClosed += (s, e) => paintedPause = false;
            }

            toolbox.Show();
            World.Editing = true;
            toolbox.Activate();
        }

        void CopyDiagnostics()
        {
            try
            {
                Clipboard.SetText(Diagnostics.Summary());
            }
            catch (Exception error)
            {
                Program.Log(error);
            }
        }

        void OpenContent()
        {
            Process.Start(new ProcessStartInfo(Store.Root) { UseShellExecute = true });
        }

        void SessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e)
        {
            if (exiting)
                return;
            try
            {
                if (overlay.InvokeRequired)
                    overlay.Invoke(new Action(ExitThread));
                else
                    ExitThread();
            }
            catch (InvalidOperationException)
            {
            }
        }

        void ChooseMonitor(string device)
        {
            if (toys != null)
            {
                toys.Close();
                toys.Dispose();
            }

            if (overlay != null)
            {
                overlay.Close();
                overlay.Dispose();
            }

            Screen screen = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == device) ?? Screen.PrimaryScreen;
            Rectangle area = testing ? new Rectangle(-10000, -10000, 1280, 752) : screen.WorkingArea;
            World.Resize(area.Width, area.Height);
            overlay = new DesktopOverlay(World, area);
            toys = new DesktopOverlay(World, area, true);
            toys.Changed += () => paintedPause = false;
            overlay.Show();
            toys.Show();
            World.Update(0);
            overlay.RenderFrame();
            toys.RenderFrame();
        }

        void ClearNotes()
        {
            deliveryRevision++;
            if (deliveryCancel != null)
            {
                deliveryCancel.Cancel();
                deliveryCancel.Dispose();
                deliveryCancel = null;
            }

            suppressRetaliation = true;
            foreach (var window in windows.ToArray())
                if (!window.IsDisposed)
                    window.Close();
            windows.Clear();
            carried = null;
            suppressRetaliation = false;
            if (World.Goose.Task == Activity.Deliver)
                World.Cancel();
        }

        internal async void BeginDelivery()
        {
            if (deliveryCancel != null)
            {
                deliveryCancel.Cancel();
                deliveryCancel.Dispose();
            }

            deliveryCancel = new CancellationTokenSource();
            var token = deliveryCancel.Token;
            int revision = ++deliveryRevision;
            string note = World.DeliveryText, path = World.DeliveryImage;
            PreparedPicture prepared = null;
            if (!String.IsNullOrEmpty(path))
                try
                {
                    prepared = await pictures.Prepare(path, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception error)
                {
                    Program.Log(error);
                    note = "Could not open that picture. Have a honk instead.";
                }

            if (exiting || revision != deliveryRevision || World.Data.Quiet || shutdown.Animating || (!String.IsNullOrEmpty(path) && World.Goose.Task != Activity.Deliver))
            {
                if (prepared != null)
                    prepared.Dispose();
                return;
            }

            windows.RemoveAll(w => w.IsDisposed);
            while (windows.Count >= 4)
            {
                suppressRetaliation = true;
                windows[0].Close();
                windows.RemoveAt(0);
                suppressRetaliation = false;
            }

            var window = new GooseWindow(note, prepared);
            carried = window;
            windows.Add(window);
            PlaceCarriedWindow();
            window.FormClosed += (s, e) =>
            {
                if (!exiting && !suppressRetaliation && !World.Data.Quiet)
                {
                    if (window == carried)
                    {
                        carried = null;
                        World.Cancel();
                    }

                    World.Chase(Pointer());
                }
            };
            window.Show();
        }

        void PlaceCarriedWindow()
        {
            if (carried == null || carried.IsDisposed)
                return;
            V beak = World.Beak(World.Goose);
            int x = overlay.Area.X + (int)beak.X - (World.DeliverySide < 0 ? carried.Width : 0), y = overlay.Area.Y + (int)beak.Y - carried.Height / 2;
            carried.Location = new Point(x, y);
        }

        void HandleCommand(int id)
        {
            paintedPause = false;
            if ((shutdown.Animating || modalDepth > 0) && id != 109)
                return;
            switch (id)
            {
                case 100:
                    World.Call(Pointer());
                    break;
                case 101:
                    World.Feed(Pointer());
                    break;
                case 102:
                    World.ThrowBall(Pointer());
                    break;
                case 103:
                    World.Fetch(Pointer());
                    break;
                case 104:
                    World.Deliver(false);
                    break;
                case 105:
                    World.Deliver(true);
                    break;
                case 106:
                    World.Honk();
                    break;
                case 107:
                    World.Paused = !World.Paused;
                    World.GrabRemaining = 0;
                    break;
                case 108:
                    SetQuiet(!World.Data.Quiet);
                    Store.Save(World.Data);
                    break;
                case 109:
                    ExitThread();
                    break;
                case 110:
                    menu.Show(Cursor.Position);
                    break;
                case 111:
                    ShowToolbox();
                    break;
                case 112:
                    OpenContent();
                    break;
                case 113:
                    contentMonitor.Reload();
                    break;
                case 115:
                    CopyDiagnostics();
                    break;
                case 114:
                    toys.ReleaseDrag();
                    World.ClearProps();
                    break;
            }
        }

        void Tick(object sender, EventArgs e)
        {
            foreach (var pair in signals)
                if (pair.Value.WaitOne(0))
                {
                    HandleCommand(CommandRegistry.Find(pair.Key).Id);
                    if (exiting)
                        return;
                }

            double now = watch.Elapsed.TotalSeconds;
            float elapsed = (float)(now - lastTime);
            lastTime = now;
            float dt = Math.Min(.1f, elapsed);
            World.AdvanceQuiet(Math.Max(0, elapsed - dt));
            bool escape = (Native.GetAsyncKeyState(27) & 0x8000) != 0;
            float previousDismiss = World.DismissRemaining;
            AdvanceDismissal(escape, elapsed);
            if (previousDismiss != World.DismissRemaining)
                paintedPause = false;
            if (exiting)
                return;
            if (shutdown.Animating)
            {
                overlay.RenderFrame();
                toys.RenderFrame();
                return;
            }

            contentMonitor.Tick();
            if (modalDepth > 0)
            {
                World.Paused = true;
                World.GrabRemaining = 0;
            }

            V pointer = Pointer();
            World.Pointer = pointer;
            bool left = (Native.GetAsyncKeyState(1) & 0x8000) != 0;
            if (left && !leftWasDown && !World.Paused && !World.Data.Quiet && !escape && !World.Objects.Any(o => o.Dragging))
            {
                if (V.Distance(pointer, World.Goose.Pos - new V(0, 18 * World.Scale)) < 32 * World.Scale)
                    World.Chase(pointer);
            }

            leftWasDown = left;
            World.Update(dt);
            if (World.HoldingCursor && !escape)
            {
                V beak = World.Beak(World.Goose);
                Native.SetCursorPos(overlay.Area.X + (int)beak.X, overlay.Area.Y + (int)beak.Y);
            }

            if (World.Goose.Task == Activity.Deliver && World.DeliveryStage == 2)
                PlaceCarriedWindow();
            try
            {
                bool needsPaint = !World.Paused || !paintedPause || World.Editing || World.DismissRemaining > 0 || escape;
                if (needsPaint)
                {
                    overlay.RenderFrame();
                    toys.RenderFrame();
                }

                paintedPause = World.Paused;
            }
            catch (Exception error)
            {
                Program.Log(error);
                ExitThread();
                return;
            }

            quietMenuIn -= dt;
            if (quietMenuIn <= 0)
            {
                quietMenuIn = 1;
                tray.Text = World.Paused ? "Goose on the Loose — paused" : World.Data.Quiet ? "Goose on the Loose — quiet" : "Goose on the Loose";
                WriteStatus();
            }
        }

        internal void AdvanceDismissal(bool escape, float elapsed)
        {
            if (escape && !shutdown.Animating)
            {
                World.GrabRemaining = 0;
                if (World.Goose.Task == Activity.Chase)
                    World.Cancel();
            }

            if (shutdown.Advance(escape, elapsed, World.Data.ReducedMotion))
            {
                World.Cancel();
                World.Paused = true;
                ClearNotes();
                toys.ReleaseDrag();
                if (toolbox != null)
                    toolbox.Close();
                if (editor != null)
                    editor.CloseForShutdown();
                World.ClearProps();
                World.Exploding = true;
                if (World.Data.Sound)
                    audio.Pop();
            }

            World.DismissRemaining = shutdown.Held > 0 && !shutdown.Animating ? 5 - shutdown.Held : 0;
            World.ExplosionTime = shutdown.EffectTime;
            if (shutdown.Finished)
                ExitThread();
        }

        internal bool IsExited
        {
            get
            {
                return exiting;
            }
        }

        internal ToolDialogOwner DialogOwner
        {
            get
            {
                return dialogOwner;
            }
        }

        internal void RunModalTool(Action<ToolDialogOwner> action)
        {
            if (exiting || shutdown.Animating || modalDepth > 0)
                return;
            bool paused = World.Paused;
            modalDepth++;
            World.Paused = true;
            World.GrabRemaining = 0;
            toys.ReleaseDrag();
            try
            {
                action(dialogOwner);
            }
            catch (Exception error)
            {
                Program.Log(error);
                if (!exiting && !dialogOwner.IsDisposed)
                    MessageBox.Show(dialogOwner, "The content tool could not finish. Your goose is still running.\r\n\r\n" + error.Message, "Could not open content tool", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                modalDepth--;
                if (!exiting)
                {
                    World.Paused = paused;
                    World.GrabRemaining = 0;
                    contentMonitor.Reload();
                }
            }
        }

        void WriteStatus()
        {
            if (String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DESKTOPGOOSE2_DATA_DIR")) && Environment.GetEnvironmentVariable("GOOSE_DIAGNOSTICS") != "1")
                return;
            try
            {
                Directory.CreateDirectory(Store.Root);
                var status = new
                {
                    Running = !exiting,
                    MainWindows = Application.OpenForms.Cast<Form>().Count(f => !(f is DesktopOverlay) && !(f is GooseWindow)),
                    OverlayWindows = Application.OpenForms.Cast<Form>().Count(f => f is DesktopOverlay),
                    GooseX = World.Goose.Pos.X,
                    GooseY = World.Goose.Pos.Y,
                    Task = World.Goose.Task.ToString(),
                    World.Paused,
                    World.Data.Quiet,
                    World.HoldingCursor,
                    World.Meals,
                    World.Fetches,
                    World.Kicks,
                    World.Deliveries,
                    GooseWindows = windows.Count(w => !w.IsDisposed),
                    MudMarks = World.Marks.Count,
                    Ball = World.BallVisible,
                    Stick = World.StickVisible,
                    World.Data.Companion,
                    Audio = audio.Backend
                };
                string path = Path.Combine(Store.Root, "runtime.json"), temp = path + ".tmp";
                File.WriteAllText(temp, new JavaScriptSerializer().Serialize(status));
                if (File.Exists(path))
                    File.Replace(temp, path, null);
                else
                    File.Move(temp, path);
            }
            catch (Exception error)
            {
                Program.Log(error);
            }
        }

        protected override void ExitThreadCore()
        {
            if (exiting)
                return;
            exiting = true;
            timer.Stop();
            Microsoft.Win32.SystemEvents.SessionEnding -= SessionEnding;
            Native.CloseOwnedDialogs(dialogOwner.Handle);
            dialogOwner.Dispose();
            commands.Dispose();
            contentMonitor.Dispose();
            pictures.Dispose();
            ClearNotes();
            if (toolbox != null)
                toolbox.Close();
            if (editor != null)
                editor.CloseForShutdown();
            if (toys != null)
            {
                toys.ReleaseDrag();
                toys.Close();
                toys.Dispose();
            }

            World.ClearProps();
            if (overlay != null)
            {
                overlay.Close();
                overlay.Dispose();
            }

            Store.Save(World.Data);
            WriteStatus();
            foreach (var signal in signals.Values)
                signal.Dispose();
            tray.Visible = false;
            tray.Icon.Dispose();
            tray.Dispose();
            menu.Dispose();
            audio.Dispose();
            timer.Dispose();
            base.ExitThreadCore();
        }

        sealed class CommandWindow : NativeWindow, IDisposable
        {
            readonly Action<int> callback;
            readonly bool testing;
            public CommandWindow(Action<int> action, bool test)
            {
                testing = test;
                callback = action;
                CreateHandle(new CreateParams { Caption = "Goose on the Loose commands", Parent = new IntPtr(-3) });
                if (!testing)
                    foreach (var c in CommandRegistry.All)
                        if (c.Key != Keys.None && !Native.RegisterHotKey(Handle, c.Id, 0x4003, (uint)c.Key))
                            Program.Log(new Exception("Shortcut already in use: Ctrl+Alt+" + c.Key));
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0x312)
                {
                    callback(m.WParam.ToInt32());
                    return;
                }

                base.WndProc(ref m);
            }

            public void Dispose()
            {
                if (!testing)
                    foreach (var c in CommandRegistry.All)
                        if (c.Key != Keys.None)
                            Native.UnregisterHotKey(Handle, c.Id);
                DestroyHandle();
            }
        }
    }
}
