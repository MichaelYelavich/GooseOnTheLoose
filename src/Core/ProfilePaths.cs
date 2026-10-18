using System;
using System.IO;

namespace SecondHonk
{
    public static class ProfilePaths
    {
        public static string Resolve(string documents)
        {
            string fresh = Path.Combine(documents, "GooseOnTheLoose"), legacy = Path.Combine(documents, "DesktopGoose2");
            if (File.Exists(Path.Combine(fresh, "settings.json")))
                return fresh;
            if (Directory.Exists(legacy) && (File.Exists(Path.Combine(legacy, "settings.json")) || Directory.Exists(Path.Combine(legacy, "Notes")) || Directory.Exists(Path.Combine(legacy, "Pictures"))))
                return legacy;
            return fresh;
        }
    }
}
