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
    public sealed class HonkAudio : IDisposable
    {
        readonly MemoryStream wave;
        readonly SoundPlayer player;
        readonly List<string> honks = new List<string>(), opened = new List<string>();
        readonly List<SoundPlayer> steps = new List<SoundPlayer>();
        readonly Random random = new Random();
        string bite, mud;
        MemoryStream popWave;
        SoundPlayer popPlayer;
        public int SuccessfulPlays
        {
            get; private set;
        }

        public string Backend
        {
            get
            {
                return honks.Count > 0 ? "Original Desktop Goose samples (WinMM)" : "Synthesized fallback";
            }
        }

        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        static extern uint mciSendString(string command, StringBuilder result, int length, IntPtr callback);
        string Open(string folder, string filename, string alias)
        {
            string path = Path.Combine(folder, filename);
            if (File.Exists(path) && mciSendString("open \"" + path + "\" type mpegvideo alias " + alias, null, 0, IntPtr.Zero) == 0)
            {
                opened.Add(alias);
                return alias;
            }

            return null;
        }

        bool PlayClip(string alias, int volume)
        {
            if (alias == null)
                return false;
            mciSendString("stop " + alias, null, 0, IntPtr.Zero);
            mciSendString("seek " + alias + " to start", null, 0, IntPtr.Zero);
            mciSendString("setaudio " + alias + " volume to " + volume, null, 0, IntPtr.Zero);
            bool success = mciSendString("play " + alias, null, 0, IntPtr.Zero) == 0;
            if (success)
                SuccessfulPlays++;
            return success;
        }

        public HonkAudio()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds");
            for (int i = 1; i <= 4; i++)
            {
                string alias = Open(folder, "Honk" + i + ".mp3", "dg2_honk" + i);
                if (alias != null)
                    honks.Add(alias);
            }

            bite = Open(folder, "BITE.mp3", "dg2_bite");
            mud = Open(folder, "MudSquith.mp3", "dg2_mud");
            for (int i = 1; i <= 3; i++)
            {
                string path = Path.Combine(folder, "Pat" + i + ".wav");
                if (File.Exists(path))
                    try
                    {
                        var step = new SoundPlayer(path);
                        step.Load();
                        steps.Add(step);
                    }
                    catch (Exception)
                    {
                    }
            }

            wave = new MemoryStream();
            var writer = new BinaryWriter(wave);
            int rate = 22050, count = (int)(rate * .26);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + count * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(rate);
            writer.Write(rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(count * 2);
            double phase = 0;
            for (int i = 0; i < count; i++)
            {
                double t = (double)i / rate, u = (double)i / count;
                phase += 2 * Math.PI * (380 - 110 * u + 24 * Math.Sin(t * 36)) / rate;
                double envelope = Math.Min(1, u * 22) * Math.Pow(1 - u, .7);
                double value = (Math.Sin(phase) + .4 * Math.Sin(phase * 2) + .2 * Math.Sin(phase * 3)) * envelope * .21;
                writer.Write((short)(value * short.MaxValue));
            }

            wave.Position = 0;
            player = new SoundPlayer(wave);
            player.Load();
        }

        public void Play()
        {
            if (honks.Count > 0 && PlayClip(honks[random.Next(honks.Count)], 650))
                return;
            try
            {
                player.Play();
            }
            catch (Exception)
            {
            }
        }

        public void Bite()
        {
            if (!PlayClip(bite, 180))
                Play();
        }

        public void Mud()
        {
            PlayClip(mud, 150);
        }

        public void Step()
        {
            if (steps.Count > 0)
                try
                {
                    steps[random.Next(steps.Count)].Play();
                }
                catch (Exception)
                {
                }
        }

        public void Pop()
        {
            try
            {
                if (popPlayer == null)
                {
                    popWave = new MemoryStream();
                    var writer = new BinaryWriter(popWave);
                    int rate = 22050, count = 3307;
                    writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(36 + count * 2);
                    writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                    writer.Write(16);
                    writer.Write((short)1);
                    writer.Write((short)1);
                    writer.Write(rate);
                    writer.Write(rate * 2);
                    writer.Write((short)2);
                    writer.Write((short)16);
                    writer.Write(Encoding.ASCII.GetBytes("data"));
                    writer.Write(count * 2);
                    for (int i = 0; i < count; i++)
                    {
                        double t = (double)i / rate, u = (double)i / count;
                        writer.Write((short)(Math.Sin(2 * Math.PI * (260 * t - 500 * t * t)) * Math.Exp(-u * 7) * 3500));
                    }

                    popWave.Position = 0;
                    popPlayer = new SoundPlayer(popWave);
                    popPlayer.Load();
                }

                popPlayer.Play();
            }
            catch (Exception)
            {
            }
        }

        public void Dispose()
        {
            foreach (string alias in opened)
                mciSendString("close " + alias, null, 0, IntPtr.Zero);
            foreach (var step in steps)
                step.Dispose();
            if (popPlayer != null)
                popPlayer.Dispose();
            if (popWave != null)
                popWave.Dispose();
            player.Dispose();
            wave.Dispose();
        }
    }
}
