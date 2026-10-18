using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
namespace SecondHonk
{
    static class SelfTest
    {
        static readonly List<string> results = new List<string>();
        static void Assert(bool value, string label)
        {
            if (!value)
                throw new Exception(label);
            results.Add("PASS  " + label);
        }
        static World New(Content c)
        {
            var w = new World(new SaveData { Mischief = false, Scale = 1 }, c, 42);
            w.Resize(1280, 752);
            w.Goose.Place(new V(450, 360));
            w.Update(0);
            return w;
        }
        static void Advance(World w, float seconds)
        {
            for (int i = 0; i < seconds * 60; i++)
                w.Update(1f / 60);
        }
        public static void Run(Content content)
        {
            Directory.CreateDirectory(Program.ArtifactsFolder);
            string root = Store.Root;
            Store.Root = Path.Combine(Program.ArtifactsFolder, "test-desktop-" + Guid.NewGuid().ToString("N"));
            try
            {
                World w = New(content);
                V p = w.Goose.Pos;
                Advance(w, 3);
                Assert(V.Distance(p, w.Goose.Pos) > 10, "Goose wanders autonomously without a game window");
                w = New(content);
                w.Feed(w.Goose.Pos + new V(130, 30));
                Advance(w, 4);
                Assert(w.Meals == 1 && !w.Objects.Any(o => o.Kind == ToyKind.Peas), "Desktop peas attract the goose and are consumed");
                w = New(content);
                w.Fetch(w.Goose.Pos + new V(220, 0));
                Advance(w, 8);
                Assert(w.Fetches == 1 && !w.CarryingStick && w.StickVisible, "Fetch retrieves the stick and drops it for reuse");
                w = New(content);
                w.Fetch(w.Goose.Pos);
                Advance(w, .1f);
                w.Chase(w.Goose.Pos + new V(300, 0));
                Assert(!w.CarryingStick && w.StickVisible, "Interrupting fetch releases the carried stick");
                w = New(content);
                w.ThrowBall(w.Goose.Pos + new V(90, 0));
                Advance(w, 6);
                Assert(w.Kicks > 0, "Goose pursues and kicks a desktop ball");
                w = New(content);
                w.MakeMud();
                Advance(w, 6);
                Assert(w.Marks.Count > 2, "Muddy footprints are emitted on planted foot landings");
                Advance(w, 20);
                Assert(w.Marks.Count == 0, "Mud footprints expire completely");
                w = New(content);
                int started = 0, finished = 0;
                V windowStart = new V();
                w.DeliveryStarted += () => { started++; windowStart = w.Goose.Pos; };
                w.DeliveryCompleted += () => finished++;
                w.Deliver(false);
                Advance(w, 18);
                Assert(started == 1 && finished == 1 && w.Deliveries == 1, "Note delivery completes its offscreen pickup and return");
                Assert(windowStart.X < 0 || windowStart.X > w.Width, "Note is created after the goose has walked beyond a desktop edge");
                w = New(content);
                w.Goose.Direction = w.Goose.HeadDirection = 0;
                w.Chase(w.Beak(w.Goose));
                w.Update(.01f);
                Assert(w.HoldingCursor, "Provoked chase catches the cursor at the beak");
                Advance(w, 2);
                Assert(!w.HoldingCursor, "Cursor theft releases within its bounded time");
                w = New(content);
                w.Chase(w.Beak(w.Goose));
                w.Update(.01f);
                w.Feed(w.Goose.Pos);
                Assert(!w.HoldingCursor && w.Goose.Task == Activity.Eat, "Feeding calms a goose and releases the cursor");
                w = New(content);
                w.Data.MouseSteal = false;
                w.Chase(w.Beak(w.Goose));
                Advance(w, 1);
                Assert(!w.HoldingCursor, "Mouse stealing can be disabled without removing chase behavior");
                w = New(content);
                w.MakeMud();
                p = w.Goose.Pos;
                w.Paused = true;
                Advance(w, 3);
                Assert(V.Distance(p, w.Goose.Pos) == 0 && !w.HoldingCursor, "Pause stops simulation and releases the cursor");
                w = New(content);
                w.SetQuiet(true);
                w.MakeMud();
                w.Deliver(false);
                w.ThrowBall(w.Goose.Pos);
                w.Chase(w.Goose.Pos);
                Advance(w, 3);
                Assert(w.Marks.Count == 0 && !w.BallVisible && !w.HoldingCursor && w.Deliveries == 0, "Quiet mode suppresses desktop pranks");
                w = New(content);
                w.SetQuiet(true);
                w.QuietRemaining = 1;
                w.Paused = true;
                Advance(w, 2);
                Assert(!w.Data.Quiet, "Timed quiet mode expires while paused");
                w = New(content);
                w.Data.Companion = true;
                w.Friend.Place(w.Goose.Pos - new V(180, 0));
                Advance(w, 4);
                Assert(V.Distance(w.Goose.Pos, w.Friend.Pos) < 180, "Second goose follows without any unlock requirements");
                w = New(content);
                w.Goose.Place(new V(10, 10));
                w.Resize(1920, 1080);
                Assert(w.Goose.Pos.X >= 35 && w.Goose.Pos.Y >= 65, "Display changes reproject the goose into a valid working area");
                w = New(content);
                for (int i = 0; i < 36000; i++)
                {
                    if (i % 1500 == 0)
                        w.MakeMud();
                    w.Update(1f / 60);
                    if (float.IsNaN(w.Goose.Pos.X) || float.IsInfinity(w.Goose.Pos.Y))
                        throw new Exception("Invalid simulation position");
                }
                Assert(w.Marks.Count <= 256, "Ten minutes of simulation maintain finite positions and bounded footprints");
                var settings = new SaveData { Companion = true, Hat = "Scarf", Scale = 2 };
                Assert(Store.Save(settings), "Initial settings save succeeds");
                settings.Hat = "Sunhat";
                Assert(Store.Save(settings) && Store.Load().Hat == "Sunhat", "Settings persist atomically");
                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "invalid");
                Assert(Store.Load().Hat == "Scarf", "Damaged settings recover the previous backup");
                string packs = Path.Combine(Store.Root, "packs");
                Directory.CreateDirectory(packs);
                File.WriteAllText(Path.Combine(packs, "bad.json"), "invalid");
                File.WriteAllText(Path.Combine(packs, "good.json"), "{\"notes\":[\"Honk\"]}");
                var testContent = new Content(packs);
                Assert(testContent.Notes.Count == 1 && testContent.Warnings.Count == 1, "Invalid content packs do not block valid packs");
                using (var bmp = new Bitmap(1280, 752))
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    w = New(content);
                    Art.Scene(g, w);
                    Assert(bmp.GetPixel(10, 10).A == 0 && bmp.GetPixel((int)w.Goose.Pos.X, (int)w.Goose.Pos.Y - 14).A > 0, "Renderer draws the goose while leaving the desktop transparent");
                }
                Extended(content);
                PatchTests.Run(content, Assert);
                BetaTests.Run(content, Assert);
                results.Add("\nAll " + results.Count + " desktop checks passed. Windows " + Environment.OSVersion.Version + ".");
                Environment.ExitCode = 0;
            }
            catch (Exception error) { results.Add("FAIL  " + error); Environment.ExitCode = 1; }
            finally { Store.Root = root; File.WriteAllLines(Path.Combine(Program.ArtifactsFolder, "desktop-self-test.txt"), results); }
        }
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int index);
        static void Extended(Content content)
        {
            var w = New(content);
            for (int i = 0; i < 16; i++)
                w.PlaceObject((ToyKind)(i % 7), new V(150 + i * 55, 300));
            Assert(w.Objects.Count == 16 && w.PlaceObject(ToyKind.Ball, new V(300, 300)) == null, "Sixteen-object cap rejects further placements");
            Assert(w.Objects.Select(o => o.Id).Distinct().Count() == 16, "Placed objects have independent stable IDs");
            var first = w.Objects[0];
            w.RemoveObject(w.Objects[1]);
            Assert(w.Objects.Contains(first) && w.Objects.Count == 15, "Removing an object preserves other instances and IDs");
            w.ClearProps();
            Assert(w.Objects.Count == 0 && w.Marks.Count == 0, "Clear removes every toy, surface and footprint");
            w = New(content);
            var ball = w.PlaceObject(ToyKind.Ball, new V(1235, 350));
            ball.Velocity = new V(900, 0);
            Advance(w, .2f);
            Assert(ball.Velocity.X < 0, "Ball bounces against the desktop edge");
            var other = w.PlaceObject(ToyKind.Ball, new V(200, 350));
            Assert(other.Id != ball.Id && w.Objects.Count == 2, "Multiple balls coexist");
            w = New(content);
            w.Data.Companion = true;
            var pond = w.PlaceObject(ToyKind.Pond, new V(450, 360));
            w.Goose.Place(pond.Pos);
            w.Friend.Place(pond.Pos + new V(20, 0));
            w.UpdateSurface(w.Goose);
            w.UpdateSurface(w.Friend);
            Assert(w.Goose.Swimming && w.Friend.Swimming && w.Goose.Coating == Coating.Wet && w.Friend.Coating == Coating.Wet, "Both geese independently float and wet their feet in ponds");
            var mud = w.PlaceObject(ToyKind.Mud, new V(850, 360));
            w.Goose.Place(mud.Pos);
            w.UpdateSurface(w.Goose);
            Assert(w.Goose.Coating == Coating.Mud && w.Friend.Coating == Coating.Wet, "Mud coating belongs to each goose individually");
            w.Goose.Place(pond.Pos);
            w.UpdateSurface(w.Goose);
            Assert(w.Goose.Coating == Coating.Wet, "Clean pond water washes mud away");
            w.Goose.Place(new V(1100, 600));
            w.Friend.Place(new V(1000, 600));
            w.UpdateSurface(w.Goose);
            w.UpdateSurface(w.Friend);
            for (int i = 0; i < 20; i++)
            {
                w.LandFoot(w.Goose, w.Goose.Pos);
                w.LandFoot(w.Friend, w.Friend.Pos);
            }
            Assert(w.Marks.Count == 40 && w.Goose.TrailRemaining == 0 && w.Friend.TrailRemaining == 0, "Both geese leave exactly twenty coated foot landings");
            w.LandFoot(w.Goose, w.Goose.Pos);
            Assert(w.Marks.Count == 40, "Dry feet stop producing footprints");
            for (int i = 0; i < 300; i++)
            {
                w.Goose.Coating = Coating.Mud;
                w.Goose.TrailRemaining = 1;
                w.LandFoot(w.Goose, w.Goose.Pos);
            }
            Assert(w.Marks.Count == 256, "All geese share the 256-print cap");
            w.Objects.Clear();
            w.Goose.Coating = w.Friend.Coating = Coating.Dry;
            w.Goose.TrailRemaining = w.Friend.TrailRemaining = 0;
            Advance(w, 11);
            Assert(w.Marks.Count == 0, "Wet and muddy footprints fade and expire after ten seconds");
            w.ClearProps();
            var puddle = w.PlaceObject(ToyKind.Puddle, w.Goose.Pos);
            w.UpdateSurface(w.Goose);
            Assert(!w.Goose.Swimming && w.Goose.Coating == Coating.Wet, "Shallow puddles wet feet without swimming");
            var duck = w.PlaceObject(ToyKind.Duck, w.Goose.Pos);
            w.Update(.05f);
            Assert(duck.Velocity.Length > 0, "Rubber ducks can be nudged by a goose");
            var clock = new ShutdownClock();
            Assert(!clock.Advance(true, 4.999f, false) && !clock.Animating, "Escape dismissal never triggers before five seconds");
            clock.Advance(false, .001f, false);
            Assert(clock.Held == 0 && !clock.Animating, "Releasing Escape cancels the countdown");
            Assert(clock.Advance(true, 5, false) && clock.Animating, "The exact five-second threshold starts dismissal");
            clock.Advance(false, .79f, false);
            Assert(!clock.Finished, "Cartoon burst remains visible during its effect");
            clock.Advance(false, .01f, false);
            Assert(clock.Finished, "Dismissal animation finishes within one second");
            clock = new ShutdownClock();
            clock.Advance(true, 5, true);
            clock.Advance(false, .25f, true);
            Assert(clock.Finished, "Reduced motion uses a quarter-second fade");
            UserContent.Initialize(AppDomain.CurrentDomain.BaseDirectory, null, false);
            var contentWorld = New(new Content(Store.Root));
            using (var monitor = new ContentMonitor(contentWorld))
            {
                string note = Path.Combine(Store.Root, "Notes", "Live.txt");
                File.WriteAllText(note, "Live reload honk");
                System.Threading.Thread.Sleep(800);
                for (int poll = 0; poll < 100 && !contentWorld.Content.Notes.Contains("Live reload honk"); poll++)
                {
                    monitor.Tick();
                    System.Threading.Thread.Sleep(10);
                }
                Assert(contentWorld.Content.Notes.Contains("Live reload honk"), "Folder watcher reloads saved notes without restarting");
                string preset = Path.Combine(Store.Root, "Behaviors", "default.json");
                File.WriteAllText(preset, "{\"TrailSteps\":7,\"FootprintSeconds\":4}");
                monitor.Reload();
                Assert(contentWorld.Behavior.TrailSteps == 7, "Valid behavior presets apply live");
                File.WriteAllText(preset, "{\"TrailSteps\":-1}");
                monitor.Reload();
                Assert(contentWorld.Behavior.TrailSteps == 7 && contentWorld.Content.Warnings.Count > 0, "Invalid presets retain the last valid configuration");
                File.WriteAllText(Path.Combine(Store.Root, "Pictures", "partial.png"), "incomplete");
                monitor.Reload();
                Assert(!contentWorld.Content.Images.Any(p => p.EndsWith("partial.png")), "Incomplete pictures are skipped safely");
                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Hat\":\"Sunhat\",\"Scale\":1.5}");
                monitor.Reload();
                Assert(contentWorld.Data.Hat == "Sunhat", "Saved preferences reload without restarting");
                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Scale\":99}");
                monitor.Reload();
                Assert(contentWorld.Data.Hat == "Sunhat" && contentWorld.Data.Scale == 1.5f, "Invalid preferences retain the last valid configuration");
                File.WriteAllText(note, "Edited by user");
                UserContent.Initialize(AppDomain.CurrentDomain.BaseDirectory, null, false);
                Assert(File.ReadAllText(note) == "Edited by user", "Seeding on upgrade preserves edited user content");
            }
            var fresh = New(content);
            Assert(fresh.Objects.Count == 0 && fresh.Marks.Count == 0, "Every new world starts with a clean desktop");
            Assert(CommandRegistry.Find("toolbox") != null && CommandRegistry.Find("clear") != null && CommandRegistry.Find("reload") != null && CommandRegistry.Find("content-folder") != null && CommandRegistry.Find("quit").Key == System.Windows.Forms.Keys.Q, "Shared command registry preserves shortcuts and adds content and toolbox commands");
            string currentRoot = Store.Root, legacyRoot = Path.Combine(currentRoot, "legacy"), redirected = Path.Combine(currentRoot, "Redirected Documents", "DesktopGoose2");
            Directory.CreateDirectory(legacyRoot);
            File.WriteAllText(Path.Combine(legacyRoot, "settings.json"), "{\"Version\":2,\"Hat\":\"Scarf\",\"Scale\":1.5}");
            Store.Root = redirected;
            UserContent.Initialize(AppDomain.CurrentDomain.BaseDirectory, legacyRoot, false);
            Assert(Store.Load().Hat == "Scarf", "Legacy settings migrate into a Documents path containing spaces");
            File.WriteAllText(Path.Combine(legacyRoot, "settings.json"), "{\"Version\":2,\"Hat\":\"Sunhat\"}");
            UserContent.Initialize(AppDomain.CurrentDomain.BaseDirectory, legacyRoot, false);
            Assert(Store.Load().Hat == "Scarf", "Existing Documents settings are never overwritten by legacy migration");
            Store.Root = currentRoot;
            string oldPictures = Path.Combine(legacyRoot, "Assets", "Memes"), oldNotes = Path.Combine(legacyRoot, "Packs", "Notes");
            Directory.CreateDirectory(oldPictures);
            Directory.CreateDirectory(oldNotes);
            using (var bmp = new Bitmap(10, 10))
                bmp.Save(Path.Combine(oldPictures, "old.png"), System.Drawing.Imaging.ImageFormat.Png);
            File.WriteAllText(Path.Combine(oldNotes, "old.txt"), "Legacy note");
            UserContent.ImportLegacyFolder(legacyRoot);
            UserContent.ImportLegacyFolder(legacyRoot);
            Assert(File.Exists(Path.Combine(Store.Root, "Pictures", "old.png")) && File.Exists(Path.Combine(Store.Root, "Pictures", "old (2).png")) && File.ReadAllText(Path.Combine(Store.Root, "Notes", "old.txt")) == "Legacy note", "Legacy portable Assets and Packs import pictures and notes without overwriting names");
            using (var session = new DesktopSession(new SaveData { Sound = false, Companion = true }, new Content(Store.Root), "", true))
            {
                session.World.PlaceObject(ToyKind.Pond, new V(300, 350));
                session.World.DeliveryText = "Shutdown test note";
                session.World.DeliveryImage = "";
                session.BeginDelivery();
                session.World.GrabRemaining = 1;
                session.World.Paused = true;
                session.AdvanceDismissal(true, .1f);
                Assert(session.World.GrabRemaining == 0 && !session.IsExited, "A short Escape releases the cursor immediately while paused");
                session.AdvanceDismissal(false, .1f);
                session.AdvanceDismissal(true, 5);
                Assert(session.World.Exploding && session.World.Data.Companion && session.World.Objects.Count == 0 && !System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().Any(f => f is GooseWindow), "Five-second exit stops paused gameplay, removes objects and closes carried windows without retaliation");
                using (var bmp = new Bitmap(1280, 752))
                using (var g = Graphics.FromImage(bmp))
                {
                    session.World.Goose.Place(new V(400, 350));
                    session.World.Friend.Place(new V(800, 350));
                    session.World.ExplosionTime = .2f;
                    Art.Scene(g, session.World);
                    Assert(bmp.GetPixel(400, 325).A > 0 && bmp.GetPixel(800, 325).A > 0, "Both active geese burst in the same rendered frame");
                    bmp.Save(Path.Combine(Program.ArtifactsFolder, "simultaneous-burst.png"));
                }
                session.AdvanceDismissal(false, .8f);
                Assert(session.IsExited && !System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().Any(), "Animated shutdown disposes every overlay and tool window");
            }
            using (var session = new DesktopSession(new SaveData { Sound = false }, new Content(Store.Root), "", true))
            {
                session.World.DeliveryText = "Shutdown test note";
                session.World.DeliveryImage = "";
                session.BeginDelivery();
                session.World.PlaceObject(ToyKind.Ball, new V(300, 350));
                session.ExitThread();
                Assert(session.IsExited && session.World.Objects.Count == 0 && !System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().Any(), "Immediate quit uses the same complete cleanup without animation");
            }
            w = New(content);
            var water = w.PlaceObject(ToyKind.Pond, new V(300, 350));
            ball = w.PlaceObject(ToyKind.Ball, new V(700, 350));
            using (var layer = new DesktopOverlay(w, new Rectangle(-10000, -10000, 1280, 752), true))
            {
                layer.Show();
                Assert(!layer.BeginDrag(water.Pos), "Water center does not consume desktop input");
                Assert(layer.BeginDrag(ball.Pos), "Toy interaction layer starts a ball drag");
                layer.MoveDrag(new V(800, 400), .05f);
                layer.ReleaseDrag();
                Assert(V.Distance(ball.Pos, new V(800, 400)) < 1 && ball.Velocity.Length > 0 && !ball.Dragging, "Dragging places and flicks a ball and releases capture");
                w.Editing = true;
                Assert(layer.BeginDrag(water.Pos - new V(0, water.Size.Y / 2)), "Opening the toolbox enables water move handles");
                layer.MoveDrag(new V(400, 350 - water.Size.Y / 2), .05f);
                layer.ReleaseDrag();
                Assert(water.Pos.X == 400 && water.Velocity.Length == 0, "Water move handles reposition without flicking");
                layer.BeginDrag(water.Pos + water.Size / 2);
                layer.MoveDrag(water.Pos + new V(170, 110), .05f);
                layer.ReleaseDrag();
                Assert(water.Size.X == 340 && water.Size.Y == 220, "Water resize handles change surface dimensions");
                layer.BeginDrag(water.Pos + new V(water.Size.X / 2, -water.Size.Y / 2));
                Assert(!w.Objects.Contains(water) && w.Objects.Contains(ball), "Remove handles delete only the selected object");
            }
            w = New(content);
            water = w.PlaceObject(ToyKind.Pond, new V(300, 350));
            using (var bmp = new Bitmap(1280, 752))
            using (var g = Graphics.FromImage(bmp))
            {
                Art.Interactions(g, w);
                Assert(bmp.GetPixel(300, 350).A == 0, "Closed-toolbox water is fully transparent in the interactive layer");
                w.Editing = true;
                Art.Interactions(g, w);
                Assert(bmp.GetPixel(300, 350).A == 0 && bmp.GetPixel(300, (int)(350 - water.Size.Y / 2)).A > 0, "Water editing draws handles while keeping the surface click-through");
            }
        }
        [DllImport("user32.dll")] static extern int GetGuiResources(IntPtr h, int flags);
        public static void NativeSmoke(Content content)
        {
            ApplicationThreadErrors();
            Directory.CreateDirectory(Program.ArtifactsFolder);
            var output = new List<string>();
            try
            {
                World w = New(content);
                using (var overlay = new DesktopOverlay(w, new Rectangle(-10000, -10000, 1280, 752)))
                {
                    overlay.Show();
                    int style = GetWindowLong(overlay.Handle, -20);
                    if ((style & 0x080800A0) != 0x080800A0)
                        throw new Exception("Incorrect overlay styles");
                    output.Add("PASS  Goose overlay is layered, click-through, no-activate and excluded from the taskbar.");
                    overlay.RenderFrame();
                    int before = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
                    var clock = Stopwatch.StartNew();
                    for (int i = 0; i < 180; i++)
                    {
                        w.Update(1f / 30);
                        overlay.RenderFrame();
                    }
                    clock.Stop();
                    int after = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
                    if (after > before + 3)
                        throw new Exception("GDI handles grew");
                    output.Add("PASS  180 native alpha frames; GDI handles " + before + " -> " + after + "; " + clock.ElapsedMilliseconds + " ms total offscreen workload.");
                    w.Goose.Place(new V(-300, 350));
                    overlay.RenderFrame();
                    if (overlay.Visible)
                        throw new Exception("Offscreen goose overlay was not hidden");
                    w.Goose.Place(new V(350, 350));
                    w.Update(0);
                    overlay.RenderFrame();
                    if (!overlay.Visible)
                        throw new Exception("Returning goose failed to reappear");
                    output.Add("PASS  Overlay disappears offscreen and reappears for the returning goose.");
                    overlay.Close();
                }
                w = New(content);
                w.Data.Companion = true;
                w.Editing = true;
                for (int i = 0; i < 16; i++)
                    w.PlaceObject((ToyKind)(i % 7), new V(120 + (i % 5) * 210, 150 + (i / 5) * 160));
                using (var bg = new DesktopOverlay(w, new Rectangle(-10000, -10000, 1280, 752)))
                using (var toys = new DesktopOverlay(w, new Rectangle(-10000, -10000, 1280, 752), true))
                {
                    bg.Show();
                    toys.Show();
                    toys.RenderFrame();
                    if ((GetWindowLong(toys.Handle, -20) & 0x20) != 0)
                        throw new Exception("Toy layer cannot receive input");
                    bg.RenderFrame();
                    int before = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
                    for (int i = 0; i < 300; i++)
                    {
                        w.Update(1f / 30);
                        bg.RenderFrame();
                        toys.RenderFrame();
                    }
                    int after = GetGuiResources(Process.GetCurrentProcess().Handle, 0);
                    if (after > before + 3)
                        throw new Exception("Full assortment leaked GDI handles");
                    output.Add("PASS  Separate interactive toy layer; 300 full-assortment dual-layer frames, GDI " + before + " -> " + after + ".");
                    using (var bmp = new Bitmap(1280, 752))
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.FromArgb(40, 54, 48));
                        Art.Scene(g, w);
                        Art.Interactions(g, w);
                        bmp.Save(Path.Combine(Program.ArtifactsFolder, "desktop-assortment.png"));
                    }
                }
                using (var box = new ToyBox(w, () => new Rectangle(0, 0, 1280, 752)))
                {
                    box.Location = new Point(-10000, -10000);
                    box.Show();
                    using (var bmp = new Bitmap(box.Width, box.Height))
                    {
                        box.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                        bmp.Save(Path.Combine(Program.ArtifactsFolder, "native-toy-box.png"));
                    }
                    box.Close();
                }
                output.Add("PASS  Native Toy Box renders illustrated controls.");
                using (var note = new GooseWindow("Honk.\r\nThis is a goose-created note.", ""))
                {
                    note.Location = new Point(-10000, -10000);
                    note.Show();
                    using (var bmp = new Bitmap(note.Width, note.Height))
                    {
                        note.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                        bmp.Save(Path.Combine(Program.ArtifactsFolder, "native-goose-note.png"));
                    }
                    note.Close();
                }
                output.Add("PASS  Goose-created note uses an ordinary native text window.");
                using (var audio = new HonkAudio())
                {
                    if (audio.Backend != "Original Desktop Goose samples (WinMM)")
                        throw new Exception("Original honk clips were not opened");
                    output.Add("PASS  Windows media backend opened the original Desktop Goose honk clips.");
                }
                PatchTests.Native(content, output);
                Environment.ExitCode = 0;
            }
            catch (Exception error) { output.Add("FAIL  " + error); Environment.ExitCode = 1; }
            File.WriteAllLines(Path.Combine(Program.ArtifactsFolder, "desktop-native-smoke.txt"), output);
        }
        static void ApplicationThreadErrors()
        {
            System.Windows.Forms.Application.ThreadException += (s, e) => { Program.Log(e.Exception); File.WriteAllText(Path.Combine(Program.ArtifactsFolder, "native-thread-error.txt"), e.Exception.ToString()); Environment.Exit(1); };
        }
    }
}
