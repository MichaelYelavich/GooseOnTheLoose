using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SecondHonk
{
    // Dev-mode readout: three read-only panels (performance, bond, state)
    // drawn by Art from live simulation state. Visuals only: no gameplay,
    // accrual or settings changes flow through this window.
    public sealed class DevOverlay : Form
    {
        readonly World world;
        float fps;
        float frameMs;
        readonly Font font = new Font("Consolas", 9, FontStyle.Regular, GraphicsUnit.Pixel);

        public DevOverlay(World world)
        {
            this.world = world;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            Width = 340;
            Height = 400;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var p = base.CreateParams;
                p.ExStyle |= 0x80000 | 0x80 | 0x20;
                return p;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }

        public void RefreshStats(float framesPerSecond, float frameMilliseconds)
        {
            fps = framesPerSecond;
            frameMs = frameMilliseconds;
            if (Visible)
                Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var background = new SolidBrush(Color.FromArgb(200, 20, 20, 20)))
                e.Graphics.FillRectangle(background, ClientRectangle);
            using (var brush = new SolidBrush(Color.FromArgb(240, 240, 240)))
            {
                float y = 6;
                foreach (string line in DevReadout.DevPanel(world, fps, frameMs))
                {
                    e.Graphics.DrawString(line, font, brush, 6, y);
                    y += 13;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                font.Dispose();
            base.Dispose(disposing);
        }
    }

    public static class DevReadout
    {
        // One text line per entry, panels separated by headers. Pure function
        // of simulation state plus the session's measured frame timing.
        public static List<string> DevPanel(World world, float fps, float frameMs)
        {
            var lines = new List<string>();
            lines.Add("--- performance ---");
            lines.Add(string.Format("fps {0:F1}  frame {1:F2} ms", fps, frameMs));
            lines.Add(string.Format("objects {0}  marks {1}", world.Objects.Count, world.Marks.Count));
            lines.Add("--- bond ---");
            lines.Add(string.Format("bond {0:F5}  scenes {1}  last {2}", world.Data.Bond, world.Scenes.CompletedScenes, world.Scenes.LastCompleted));
            lines.Add(string.Format("accept +{0:F3}  return-honk {1:F2}  partner-note {2:F2}", .1f * world.Data.Bond, .25f + .5f * world.Data.Bond, .12f + .5f * world.Data.Bond));
            if (world.BondLog.Count == 0)
                lines.Add("(no accruals yet)");
            else
                foreach (string entry in world.BondLog)
                    lines.Add(entry);
            lines.Add("--- state ---");
            lines.Add(string.Format("mode {0}  paused {1}", world.Data.Mode, world.Paused));
            string scene = world.Scenes.Active == null ? "none" : world.Scenes.Active.Kind + " " + world.Scenes.Active.Phase;
            lines.Add("scene " + scene);
            lines.Add("broker " + BrokerLine(world));
            lines.Add(string.Format("cursor {0}  delivery {1}  window {2}", OwnerName(world.Coordinator.CursorOwner), OwnerName(world.Coordinator.DeliveryOwner), OwnerName(world.Coordinator.WindowOwner)));
            return lines;
        }

        static string OwnerName(Goose owner)
        {
            return owner != null ? owner.Name : "-";
        }

        static string BrokerLine(World world)
        {
            var broker = world.Broker;
            string active = broker.Active == null ? "-" : broker.Active.Kind + " " + broker.Active.Elapsed.ToString("F2") + "/" + broker.Active.Duration.ToString("F2") + "s";
            string pending = broker.Pending == null ? "-" : broker.Pending.Kind.ToString();
            return string.Format("active {0}  pending {1}  cooldown {2:F1}s", active, pending, broker.CooldownRemaining);
        }
    }
}
