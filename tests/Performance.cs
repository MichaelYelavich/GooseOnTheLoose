using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SecondHonk
{
    public static class Performance
    {
        [DllImport("user32.dll")]
        static extern int GetGuiResources(IntPtr process, int flags);
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            string output = args.Length > 0 ? args[0] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "performance.txt");
            double minutes = args.Length > 1 ? double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 0;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var lines = new List<string>();
            try
            {
                var content = new Content(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Packs"));
                foreach (string scenario in new[]
                {
                    "idle",
                    "pair",
                    "full"
                }

                )
                {
                    var world = new World(new SaveData { Sound = false, Mischief = false, Companion = scenario != "idle" }, content, 42);
                    world.Goose.Place(new V(450, 350));
                    world.Friend.Place(new V(600, 350));
                    world.Editing = scenario == "full";
                    if (scenario == "full")
                    {
                        for (int i = 0; i < 16; i++)
                            world.PlaceObject((ToyKind)(i % 7), new V(130 + i % 5 * 200, 150 + i / 5 * 150));
                        Populate(world);
                    }

                    using (var overlay = new DesktopOverlay(world, new Rectangle(-10000, -10000, 1280, 752)))
                    using (var toys = new DesktopOverlay(world, new Rectangle(-10000, -10000, 1280, 752), true))
                    using (var process = Process.GetCurrentProcess())
                    {
                        overlay.Show();
                        toys.Show();
                        for (int i = 0; i < 20; i++)
                        {
                            world.Update(1f / 30);
                            overlay.RenderFrame();
                            toys.RenderFrame();
                        }

                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        process.Refresh();
                        int gdi = GetGuiResources(process.Handle, 0), user = GetGuiResources(process.Handle, 1), collections = GC.CollectionCount(0);
                        long memory = process.PrivateMemorySize64;
                        TimeSpan cpu = process.TotalProcessorTime;
                        var samples = new List<double>();
                        var total = Stopwatch.StartNew();
                        int frames = 0;
                        do
                        {
                            var frame = Stopwatch.StartNew();
                            world.Update(1f / 30);
                            if (scenario == "full" && world.Marks.Count < 256)
                                Populate(world);
                            overlay.RenderFrame();
                            toys.RenderFrame();
                            frame.Stop();
                            samples.Add(frame.Elapsed.TotalMilliseconds);
                            frames++;
                            if (samples.Count > 60000)
                                samples.RemoveRange(0, 30000);
                            if (minutes > 0 && scenario == "full")
                            {
                                if (frames % 300 == 0)
                                {
                                    using (var window = new GooseWindow("Soak note", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Packs", "Images", "honk.png")))
                                    {
                                        window.Location = new Point(-10000, -10000);
                                        window.Show();
                                        Application.DoEvents();
                                        window.Close();
                                    }
                                }

                                if (frames % 900 == 0)
                                {
                                    process.Refresh();
                                    File.AppendAllText(output + ".progress", total.Elapsed.ToString() + " frames=" + frames + " GDI=" + GetGuiResources(process.Handle, 0) + " USER=" + GetGuiResources(process.Handle, 1) + " privateMB=" + process.PrivateMemorySize64 / 1048576 + Environment.NewLine);
                                }

                                Application.DoEvents();
                                Thread.Sleep(Math.Max(1, 33 - (int)frame.ElapsedMilliseconds));
                            }
                        }
                        while (minutes > 0 && scenario == "full" ? total.Elapsed.TotalMinutes < minutes : frames < 600);
                        total.Stop();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        process.Refresh();
                        samples.Sort();
                        int finalGdi = GetGuiResources(process.Handle, 0), finalUser = GetGuiResources(process.Handle, 1);
                        lines.Add(scenario + ": frames=" + frames + " p50_ms=" + samples[samples.Count / 2].ToString("F3") + " p95_ms=" + samples[(int)(samples.Count * .95)].ToString("F3") + " CPU_ms=" + (process.TotalProcessorTime - cpu).TotalMilliseconds.ToString("F0") + " private_MB=" + (memory / 1048576) + "->" + (process.PrivateMemorySize64 / 1048576) + " GDI=" + gdi + "->" + finalGdi + " USER=" + user + "->" + finalUser + " gen0_collections=" + (GC.CollectionCount(0) - collections) + " elapsed_s=" + total.Elapsed.TotalSeconds.ToString("F1"));
                        if (finalGdi > gdi + 4 || finalUser > user + 4)
                            throw new Exception("Unbounded native resource growth in " + scenario);
                    }

                    File.WriteAllLines(output, lines);
                }

                lines.Add("PASS: finite simulation and bounded resources.");
            }
            catch (Exception error)
            {
                lines.Add("FAIL: " + error);
                Environment.ExitCode = 1;
            }

            File.WriteAllLines(output, lines);
        }

        static void Populate(World world)
        {
            world.Marks.Clear();
            for (int i = 0; i < 256; i++)
                world.Marks.Add(new Mark { Pos = new V(100 + i % 16 * 65, 110 + i / 16 * 32), Angle = i * .1f, Scale = i % 2 == 0 ? 1.5f : 1.275f, Coating = i % 2 == 0 ? Coating.Mud : Coating.Wet });
        }
    }
}
