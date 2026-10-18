using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace SecondHonk
{
    public sealed class SaveData
    {
        public int Version = 2;
        public bool Sound = true, Mischief = true, RandomAttacks = false, MouseSteal = true, Companion = false, Quiet = false, ReducedMotion = false;
        public string Hat = "None", Monitor = "";
        public float Scale = 1.5f;
    }
    public static class Store
    {
        public static string Root = ProfilePaths.Resolve(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        public static string Warning = "";
        public static SaveData Load()
        {
            string p = Path.Combine(Root, "settings.json");
            foreach (string f in new[] { p, p + ".bak" })
                if (File.Exists(f))
                    try
                    {
                        SaveData s = new JavaScriptSerializer().Deserialize<SaveData>(File.ReadAllText(f));
                        if (s == null || s.Version != 2)
                            throw new InvalidDataException("Unknown settings version");
                        if (!new[] { "None", "Scarf", "Sunhat" }.Contains(s.Hat))
                            s.Hat = "None";
                        if (float.IsNaN(s.Scale) || float.IsInfinity(s.Scale))
                            s.Scale = 1.5f;
                        s.Scale = Math.Max(1, Math.Min(2, s.Scale));
                        if (f.EndsWith(".bak"))
                            Warning = "Recovered the previous settings backup.";
                        return s;
                    }
                    catch (Exception) { Warning = "Settings could not be read. Defaults are in use; old settings were preserved."; }
            return new SaveData();
        }
        public static bool Save(SaveData data)
        {
            try
            {
                Directory.CreateDirectory(Root);
                string p = Path.Combine(Root, "settings.json"), temp = p + ".tmp";
                File.WriteAllText(temp, new JavaScriptSerializer().Serialize(data));
                if (File.Exists(p))
                    File.Replace(temp, p, p + ".bak", true);
                else
                    File.Move(temp, p);
                return true;
            }
            catch (Exception e) { Warning = e.Message; return false; }
        }
    }
}
