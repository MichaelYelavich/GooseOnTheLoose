using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SecondHonk
{
    public sealed class NoteEditor : Form
    {
        readonly ListBox list = new ListBox
        {
            Dock = DockStyle.Left,
            Width = 150
        };
        readonly TextBox text = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            MaxLength = 1000,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 11)
        };
        readonly Label status = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 38,
            Padding = new Padding(6)
        };
        readonly Timer saveTimer = new Timer
        {
            Interval = 1000
        };
        readonly string folder = Path.Combine(Store.Root, "Notes");
        string current, original = "";
        bool loading, dirty, silent;
        public NoteEditor()
        {
            Text = "Goose notes";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(540, 360);
            MinimumSize = new Size(420, 300);
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                Padding = new Padding(6)
            };
            var add = new Button
            {
                Text = "New note",
                AutoSize = true
            };
            var save = new Button
            {
                Text = "Save",
                AutoSize = true
            };
            bar.Controls.Add(add);
            bar.Controls.Add(save);
            Controls.Add(text);
            Controls.Add(list);
            Controls.Add(status);
            Controls.Add(bar);
            text.TextChanged += (s, e) =>
            {
                if (!loading)
                {
                    dirty = true;
                    saveTimer.Stop();
                    saveTimer.Start();
                }
            };
            saveTimer.Tick += (s, e) =>
            {
                saveTimer.Stop();
                Save();
            };
            list.SelectedIndexChanged += (s, e) =>
            {
                if (loading)
                    return;
                string selected = list.SelectedItem as string;
                if (!Save())
                {
                    loading = true;
                    list.SelectedItem = current;
                    loading = false;
                    return;
                }

                loading = true;
                list.SelectedItem = selected;
                loading = false;
                LoadSelected();
            };
            add.Click += (s, e) =>
            {
                if (!Save())
                    return;
                try
                {
                    Directory.CreateDirectory(folder);
                    string name = "Note-" + Guid.NewGuid().ToString("N") + ".txt";
                    Pictures.Publish(name, System.Text.Encoding.UTF8.GetBytes("Honk."), folder, System.Threading.CancellationToken.None);
                    list.Items.Add(name);
                    list.SelectedItem = name;
                }
                catch (Exception error)
                {
                    Report(error);
                }
            };
            save.Click += (s, e) => Save();
            FormClosing += (s, e) =>
            {
                if (!Save() && !silent)
                    e.Cancel = true;
            };
            try
            {
                Directory.CreateDirectory(folder);
                foreach (string path in Directory.GetFiles(folder, "*.txt"))
                    list.Items.Add(Path.GetFileName(path));
                if (list.Items.Count > 0)
                    list.SelectedIndex = 0;
            }
            catch (Exception error)
            {
                Report(error);
            }
        }

        void LoadSelected()
        {
            current = list.SelectedItem as string;
            loading = true;
            dirty = false;
            try
            {
                original = current == null ? "" : File.ReadAllText(Path.Combine(folder, current));
                text.Text = original;
                text.Enabled = true;
                status.Text = "Changes save automatically.";
            }
            catch (Exception error)
            {
                current = null;
                original = text.Text = "";
                text.Enabled = false;
                Report(error);
            }
            finally
            {
                loading = false;
            }
        }

        bool Save()
        {
            saveTimer.Stop();
            if (current == null || !dirty)
                return true;
            try
            {
                bool conflict;
                string saved = AtomicNotes.Save(Path.Combine(folder, current), original, text.Text, out conflict);
                original = text.Text;
                dirty = false;
                if (conflict)
                {
                    current = Path.GetFileName(saved);
                    loading = true;
                    list.Items.Add(current);
                    list.SelectedItem = current;
                    loading = false;
                    status.Text = "Another editor changed the file. Your changes are in a conflict copy.";
                }
                else
                    status.Text = "Saved.";
                return true;
            }
            catch (Exception error)
            {
                Report(error);
                return false;
            }
        }

        void Report(Exception error)
        {
            Program.Log(error);
            status.Text = "Could not read or save this note. " + error.Message;
        }

        public void CloseForShutdown()
        {
            silent = true;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                saveTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
