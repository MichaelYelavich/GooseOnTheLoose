using System;

namespace SecondHonk
{
    public enum OutfitSlot
    {
        Head,
        Neck,
        Glasses
    }

    // One goose's visible outfit. Every slot combines with the others; the
    // legacy shared choice (None, Scarf, Sunhat) maps onto head/neck with
    // empty glasses. New values (Beanie, BowTie, Round) have no legacy
    // equivalent and are carried by the per-goose fields only.
    public sealed class Outfit
    {
        public string Head = "None", Neck = "None", Glasses = "None";

        public Outfit Clone()
        {
            return new Outfit { Head = Head, Neck = Neck, Glasses = Glasses };
        }

        public bool Equals(Outfit other)
        {
            return other != null && Head == other.Head && Neck == other.Neck && Glasses == other.Glasses;
        }
    }

    public static class OutfitCatalog
    {
        public static readonly string[] Heads = { "None", "Sunhat", "Beanie" };
        public static readonly string[] Necks = { "None", "Scarf", "BowTie" };
        public static readonly string[] Glasses = { "None", "Round" };

        public static bool ValidHead(string value)
        {
            return value == "None" || value == "Sunhat" || value == "Beanie";
        }

        public static bool ValidNeck(string value)
        {
            return value == "None" || value == "Scarf" || value == "BowTie";
        }

        public static bool ValidGlasses(string value)
        {
            return value == "None" || value == "Round";
        }

        public static bool ValidSlot(OutfitSlot slot, string value)
        {
            switch (slot)
            {
                case OutfitSlot.Head:
                    return ValidHead(value);
                case OutfitSlot.Neck:
                    return ValidNeck(value);
                default:
                    return ValidGlasses(value);
            }
        }

        public static void ValidateSlot(OutfitSlot slot, string value)
        {
            if (!ValidSlot(slot, value))
                throw new ArgumentException("Unknown outfit value: " + (value ?? ""));
        }

        // Best-effort legacy alias for the old shared Hat preference.
        // Representable matching outfits round-trip exactly; anything newer
        // (a beanie, a bow tie, round glasses, or geese dressed differently)
        // falls back to White's closest legacy equivalent. Always returns one
        // of None, Scarf or Sunhat so old validation keeps working.
        public static string LegacyAlias(Outfit white, Outfit canada)
        {
            if (white != null && white.Equals(canada))
            {
                if (white.Head == "None" && white.Neck == "None" && white.Glasses == "None")
                    return "None";
                if (white.Head == "Sunhat" && white.Neck == "None" && white.Glasses == "None")
                    return "Sunhat";
                if (white.Head == "None" && white.Neck == "Scarf" && white.Glasses == "None")
                    return "Scarf";
            }

            if (white != null)
            {
                if (white.Neck == "Scarf")
                    return "Scarf";
                if (white.Head == "Sunhat")
                    return "Sunhat";
            }

            return "None";
        }
    }
}
