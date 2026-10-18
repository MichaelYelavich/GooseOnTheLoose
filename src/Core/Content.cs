using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace SecondHonk
{
    public sealed class ContentPack
    {
        public string name; public string[] notes;
    }
    public sealed class Content
    {
        public readonly List<string> Notes = new List<string>(), Images = new List<string>(), Warnings = new List<string>();
        public Content(string directory)
        {
            Reload(directory);
        }
        public void Reload(string directory)
        {
            Notes.Clear();
            Images.Clear();
            Warnings.Clear();
            if (Directory.Exists(directory))
                foreach (string path in Directory.GetFiles(directory, "*.json").Where(p => Path.GetFileName(p) != "settings.json" && Path.GetFileName(p) != "runtime.json").OrderBy(p => p))
                    try
                    {
                        if (new FileInfo(path).Length > 131072)
                            throw new InvalidDataException("Pack exceeds 128 KB");
                        var pack = new JavaScriptSerializer().Deserialize<ContentPack>(File.ReadAllText(path));
                        if (pack == null || pack.notes == null || pack.notes.Length > 100)
                            throw new InvalidDataException("Expected a notes array (up to 100 entries)");
                        Notes.AddRange(pack.notes.Where(n => !String.IsNullOrWhiteSpace(n) && n.Length <= 1000));
                    }
                    catch (Exception e) { Warnings.Add(Path.GetFileName(path) + ": " + e.Message); }
            string texts = Path.Combine(directory, "Notes");
            if (Directory.Exists(texts))
                foreach (string path in Directory.GetFiles(texts, "*.txt").OrderBy(x => x))
                    try
                    {
                        if (new FileInfo(path).Length <= 65536)
                        {
                            string text = File.ReadAllText(path);
                            if (!String.IsNullOrWhiteSpace(text) && text.Length <= 1000)
                                Notes.Add(text);
                        }
                    }
                    catch (Exception e) { Warnings.Add(Path.GetFileName(path) + ": " + e.Message); }
            foreach (string name in new[] { "Pictures", "Images" })
            {
                string images = Path.Combine(directory, name);
                if (Directory.Exists(images))
                    foreach (string p in Directory.GetFiles(images).OrderBy(x => x))
                        try
                        {
                            if (new[] { ".png", ".jpg", ".jpeg", ".gif" }.Contains(Path.GetExtension(p).ToLowerInvariant()) && new FileInfo(p).Length <= 8388608)
                                using (var image = System.Drawing.Image.FromFile(p))
                                {
                                    if (image.Width <= 8192 && image.Height <= 8192)
                                        Images.Add(p);
                                }
                        }
                        catch (Exception e) { Warnings.Add(Path.GetFileName(p) + ": " + e.Message); }
            }
            if (Notes.Count == 0)
                Notes.Add("I have stolen this window.\r\nHonk.");
        }
    }
}
