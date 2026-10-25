using System;
using System.IO;
using System.Web.Script.Serialization;

namespace SecondHonk
{
    public sealed class SaveData
    {
        public int Version = 2;
        public bool Sound = true, Mischief = true, RandomAttacks = false, MouseSteal = true, Companion = false, Quiet = false, ReducedMotion = false;
        public string Monitor = "";
        public string Mode = "Normal", PreviousMode = "Normal";
        public float Scale = 1.5f;
        // Pair bond, 0 (new pair) to 1 (fully warm). Invisible: no meter,
        // no readout, no chores. Grows only through varied shared play;
        // missing in upgraded profiles (compatible default 0).
        public float Bond = 0;
        // Wardrobe, Card 4: three accessory slots per goose. Linked by
        // default: a linked change applies to both geese unless that goose's
        // slot was individually set (the *Own flags). Unlinking keeps the
        // outfits on show; relinking copies one goose's outfit to both and
        // clears the flags.
        public bool LinkedOutfits = true;
        public string WhiteHead = "None", WhiteNeck = "None", WhiteGlasses = "None";
        public string CanadaHead = "None", CanadaNeck = "None", CanadaGlasses = "None";
        public bool WhiteHeadOwn, WhiteNeckOwn, WhiteGlassesOwn;
        public bool CanadaHeadOwn, CanadaNeckOwn, CanadaGlassesOwn;

        // Legacy shared-appearance alias. Not serialized: modern files carry
        // the per-goose slots, and legacy files are migrated by peeking at
        // the raw JSON (see MigrateLegacyJson). The setter applies eagerly so
        // in-memory and upgraded code keeps working; the getter derives the
        // closest legacy value (see OutfitCatalog.LegacyAlias).
        [ScriptIgnore]
        public string Hat
        {
            get
            {
                return OutfitCatalog.LegacyAlias(GetOutfit(0), GetOutfit(1));
            }

            set
            {
                ApplyLegacyHat(value);
            }
        }

        public bool Passive
        {
            get
            {
                return Mode == "Passive";
            }
        }

        static string SlotValue(SaveData data, int goose, OutfitSlot slot)
        {
            if (goose == 0)
                return slot == OutfitSlot.Head ? data.WhiteHead : slot == OutfitSlot.Neck ? data.WhiteNeck : data.WhiteGlasses;
            return slot == OutfitSlot.Head ? data.CanadaHead : slot == OutfitSlot.Neck ? data.CanadaNeck : data.CanadaGlasses;
        }

        static void SetSlotValue(SaveData data, int goose, OutfitSlot slot, string value)
        {
            if (goose == 0)
            {
                if (slot == OutfitSlot.Head)
                    data.WhiteHead = value;
                else if (slot == OutfitSlot.Neck)
                    data.WhiteNeck = value;
                else
                    data.WhiteGlasses = value;
                return;
            }

            if (slot == OutfitSlot.Head)
                data.CanadaHead = value;
            else if (slot == OutfitSlot.Neck)
                data.CanadaNeck = value;
            else
                data.CanadaGlasses = value;
        }

        static bool SlotOwned(SaveData data, int goose, OutfitSlot slot)
        {
            if (goose == 0)
                return slot == OutfitSlot.Head ? data.WhiteHeadOwn : slot == OutfitSlot.Neck ? data.WhiteNeckOwn : data.WhiteGlassesOwn;
            return slot == OutfitSlot.Head ? data.CanadaHeadOwn : slot == OutfitSlot.Neck ? data.CanadaNeckOwn : data.CanadaGlassesOwn;
        }

        static void SetSlotOwned(SaveData data, int goose, OutfitSlot slot, bool owned)
        {
            if (goose == 0)
            {
                if (slot == OutfitSlot.Head)
                    data.WhiteHeadOwn = owned;
                else if (slot == OutfitSlot.Neck)
                    data.WhiteNeckOwn = owned;
                else
                    data.WhiteGlassesOwn = owned;
                return;
            }

            if (slot == OutfitSlot.Head)
                data.CanadaHeadOwn = owned;
            else if (slot == OutfitSlot.Neck)
                data.CanadaNeckOwn = owned;
            else
                data.CanadaGlassesOwn = owned;
        }

        static void CheckGoose(int goose)
        {
            if (goose != 0 && goose != 1)
                throw new ArgumentException("Unknown goose slot: " + goose);
        }

        public Outfit GetOutfit(int goose)
        {
            CheckGoose(goose);
            return new Outfit
            {
                Head = SlotValue(this, goose, OutfitSlot.Head),
                Neck = SlotValue(this, goose, OutfitSlot.Neck),
                Glasses = SlotValue(this, goose, OutfitSlot.Glasses)
            };
        }

        public string GetOutfitSlot(int goose, OutfitSlot slot)
        {
            CheckGoose(goose);
            return SlotValue(this, goose, slot);
        }

        public bool SlotIndividuallySet(int goose, OutfitSlot slot)
        {
            CheckGoose(goose);
            return SlotOwned(this, goose, slot);
        }

        // Dress one goose. Marks that goose's slot as individually set, so
        // later linked changes leave it alone.
        public void SetOutfitSlot(int goose, OutfitSlot slot, string value)
        {
            CheckGoose(goose);
            OutfitCatalog.ValidateSlot(slot, value);
            SetSlotValue(this, goose, slot, value);
            SetSlotOwned(this, goose, slot, true);
        }

        // Linked change: applies to both geese unless that goose's slot was
        // individually set. Never sets or clears the individual flags.
        public void SetLinkedSlot(OutfitSlot slot, string value)
        {
            OutfitCatalog.ValidateSlot(slot, value);
            for (int goose = 0; goose < 2; goose++)
                if (!SlotOwned(this, goose, slot))
                    SetSlotValue(this, goose, slot, value);
        }

        // Unlinking keeps the outfits on show. Relinking copies the source
        // goose's full outfit onto the other goose and clears every
        // individual flag, so both match from then on.
        public void SetOutfitsLinked(bool linked, int sourceGoose)
        {
            if (!linked)
            {
                LinkedOutfits = false;
                return;
            }

            CheckGoose(sourceGoose);
            for (int goose = 0; goose < 2; goose++)
                foreach (OutfitSlot slot in new[] { OutfitSlot.Head, OutfitSlot.Neck, OutfitSlot.Glasses })
                {
                    SetSlotValue(this, goose, slot, SlotValue(this, sourceGoose, slot));
                    SetSlotOwned(this, goose, slot, false);
                }

            LinkedOutfits = true;
        }

        // Legacy shared choice: one value dressed both geese alike.
        public void ApplyLegacyHat(string hat)
        {
            string value = hat == "Scarf" ? "Scarf" : hat == "Sunhat" ? "Sunhat" : "None";
            WhiteHead = CanadaHead = value == "Sunhat" ? "Sunhat" : "None";
            WhiteNeck = CanadaNeck = value == "Scarf" ? "Scarf" : "None";
            WhiteGlasses = CanadaGlasses = "None";
            LinkedOutfits = true;
            WhiteHeadOwn = WhiteNeckOwn = WhiteGlassesOwn = false;
            CanadaHeadOwn = CanadaNeckOwn = CanadaGlassesOwn = false;
        }

        // Legacy files name the shared Hat but no per-goose slots. Modern
        // files name the slots, so Hat (no longer serialized) never reaches
        // the deserializer from our own saves; only peek at the raw text.
        public static void MigrateLegacyJson(SaveData data, string json)
        {
            if (data == null || String.IsNullOrEmpty(json))
                return;
            if (json.Contains("\"WhiteHead\"") || json.Contains("\"CanadaHead\"") || json.Contains("\"WhiteNeck\"") || json.Contains("\"LinkedOutfits\""))
                return;
            int key = json.IndexOf("\"Hat\"");
            if (key < 0)
                return;
            int colon = json.IndexOf(':', key + 5);
            if (colon < 0)
                return;
            int open = json.IndexOf('"', colon + 1);
            if (open < 0)
                return;
            int close = json.IndexOf('"', open + 1);
            if (close < 0)
                return;
            string hat = json.Substring(open + 1, close - open - 1);
            if (hat == "Scarf" || hat == "Sunhat")
                data.ApplyLegacyHat(hat);
        }

        // Beta 2 saves name the head-slot value Beanie; the option is
        // renamed to Toque. Rewrite before validating so Beta 2 outfits
        // keep working instead of falling back to the backup.
        public static void MigrateRenamedSlots(SaveData data)
        {
            if (data == null)
                return;
            if (data.WhiteHead == "Beanie")
                data.WhiteHead = "Toque";
            if (data.CanadaHead == "Beanie")
                data.CanadaHead = "Toque";
        }

        public void ValidateSlots()
        {
            if (!OutfitCatalog.ValidHead(WhiteHead) || !OutfitCatalog.ValidNeck(WhiteNeck) || !OutfitCatalog.ValidGlasses(WhiteGlasses) || !OutfitCatalog.ValidHead(CanadaHead) || !OutfitCatalog.ValidNeck(CanadaNeck) || !OutfitCatalog.ValidGlasses(CanadaGlasses))
                throw new InvalidDataException("Unknown outfit");
        }

        void CoerceSlots()
        {
            if (!OutfitCatalog.ValidHead(WhiteHead))
                WhiteHead = "None";
            if (!OutfitCatalog.ValidNeck(WhiteNeck))
                WhiteNeck = "None";
            if (!OutfitCatalog.ValidGlasses(WhiteGlasses))
                WhiteGlasses = "None";
            if (!OutfitCatalog.ValidHead(CanadaHead))
                CanadaHead = "None";
            if (!OutfitCatalog.ValidNeck(CanadaNeck))
                CanadaNeck = "None";
            if (!OutfitCatalog.ValidGlasses(CanadaGlasses))
                CanadaGlasses = "None";
        }

        public void Normalize()
        {
            if (Mode != "Passive" && Mode != "Normal" && Mode != "Chaos")
                Mode = "Normal";
            if (PreviousMode != "Normal" && PreviousMode != "Chaos")
                PreviousMode = "Normal";
            Quiet = Mode == "Passive";
            CoerceSlots();
        }
    }

    public static class Store
    {
        public static string Root = ProfilePaths.Resolve(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        public static string Warning = "";
        public static SaveData Load()
        {
            string p = Path.Combine(Root, "settings.json");
            foreach (string f in new[]
            {
                p,
                p + ".bak"
            }

            )
                if (File.Exists(f))
                    try
                    {
                        string json = File.ReadAllText(f);
                        SaveData s = new JavaScriptSerializer().Deserialize<SaveData>(json);
                        if (s == null || s.Version != 2)
                            throw new InvalidDataException("Unknown settings version");
                        // Legacy files name the shared Hat but no per-goose
                        // slots; migrate before validating the new fields.
                        SaveData.MigrateLegacyJson(s, json);
                        SaveData.MigrateRenamedSlots(s);
                        s.ValidateSlots();
                        if (s.Mode != null && s.Mode != "Passive" && s.Mode != "Normal" && s.Mode != "Chaos")
                            throw new InvalidDataException("Unknown mode");
                        if (float.IsNaN(s.Bond) || float.IsInfinity(s.Bond) || s.Bond < 0 || s.Bond > 1)
                            throw new InvalidDataException("Unknown bond");
                        if (s.PreviousMode != null && s.PreviousMode != "Normal" && s.PreviousMode != "Chaos")
                            throw new InvalidDataException("Unknown previous mode");
                        s.Normalize();
                        if (float.IsNaN(s.Scale) || float.IsInfinity(s.Scale))
                            s.Scale = 1.5f;
                        s.Scale = Math.Max(1, Math.Min(2, s.Scale));
                        if (f.EndsWith(".bak"))
                            Warning = "Recovered the previous settings backup.";
                        return s;
                    }
                    catch (Exception)
                    {
                        Warning = "Settings could not be read. Defaults are in use; old settings were preserved.";
                    }

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
            catch (Exception e)
            {
                Warning = e.Message;
                return false;
            }
        }
    }
}
