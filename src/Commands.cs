using System;
using System.Linq;
using System.Windows.Forms;

namespace SecondHonk
{
    public sealed class GooseCommand
    {
        public string Name;
        public int Id;
        public Keys Key;
        public GooseCommand(string name, int id, Keys key)
        {
            Name = name;
            Id = id;
            Key = key;
        }
    }

    public static class CommandRegistry
    {
        public static readonly GooseCommand[] All =
        {
            new GooseCommand("come", 100, Keys.G),
            new GooseCommand("peas", 101, Keys.F),
            new GooseCommand("ball", 102, Keys.B),
            new GooseCommand("fetch", 103, Keys.T),
            new GooseCommand("note", 104, Keys.N),
            new GooseCommand("image", 105, Keys.I),
            new GooseCommand("honk", 106, Keys.H),
            new GooseCommand("pause", 107, Keys.P),
            new GooseCommand("quiet", 108, Keys.K),
            new GooseCommand("quit", 109, Keys.Q),
            new GooseCommand("menu", 110, Keys.None),
            new GooseCommand("toolbox", 111, Keys.None),
            new GooseCommand("content-folder", 112, Keys.None),
            new GooseCommand("reload", 113, Keys.None),
            new GooseCommand("clear", 114, Keys.None),
            new GooseCommand("diagnostics", 115, Keys.None)
        };
        public static GooseCommand Find(string name)
        {
            return All.FirstOrDefault(c => c.Name == name);
        }

        public static string Prefix
        {
            get
            {
                string path = Environment.GetEnvironmentVariable("DESKTOPGOOSE2_DATA_DIR");
                if (String.IsNullOrEmpty(path))
                    return "Local\\DesktopGoose2.";
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(System.IO.Path.GetFullPath(path).ToUpperInvariant()));
                    return "Local\\DesktopGoose2.Test." + BitConverter.ToString(hash, 0, 8).Replace("-", "") + ".";
                }
            }
        }
    }
}
