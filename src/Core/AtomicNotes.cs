using System;
using System.IO;

namespace SecondHonk
{
    // The editor owns the original text. An external change produces a new file,
    // never an overwrite of a file another editor has changed or removed.
    public static class AtomicNotes
    {
        public static string Save(string path, string original, string value, out bool conflict)
        {
            string folder = Path.GetDirectoryName(path);
            Directory.CreateDirectory(folder);
            conflict = !File.Exists(path) || File.ReadAllText(path) != original;
            string destination = conflict ? Path.Combine(folder, Path.GetFileNameWithoutExtension(path) + "-conflict-" + Guid.NewGuid().ToString("N") + ".txt") : path;
            string temp = Path.Combine(folder, ".note-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                File.WriteAllText(temp, value);
                if (conflict)
                    File.Move(temp, destination);
                else
                {
                    // Check immediately before atomic replacement as well.
                    if (!File.Exists(path) || File.ReadAllText(path) != original)
                    {
                        conflict = true;
                        destination = Path.Combine(folder, Path.GetFileNameWithoutExtension(path) + "-conflict-" + Guid.NewGuid().ToString("N") + ".txt");
                        File.Move(temp, destination);
                    }
                    else
                        File.Replace(temp, path, null);
                }

                return destination;
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }
    }
}
