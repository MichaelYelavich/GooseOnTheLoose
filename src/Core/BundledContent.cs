using System;

namespace SecondHonk
{
    public enum NoteTone
    {
        Cozy,
        Affectionate,
        Romantic
    }

    public sealed class BundledNote
    {
        public string Text;
        public NoteTone Tone;
    }

    // Bundled goose-to-goose correspondence. Each entry carries its tone as
    // metadata; the audience is always the sender's partner, never the user.
    // Imported pictures and notes stay user-facing by default because partner
    // selection draws only from this catalog, never from Content.Notes.
    // Text only: no artwork that could be mistaken for a third live goose.
    public static class BundledContent
    {
        // A bundled note that must always arrive as a text window, even when
        // a picture was requested alongside it.
        public const string TextOnlyMarker = "THIS COULD HAVE BEEN A HONK";

        public static readonly BundledNote[] PartnerNotes =
        {
            new BundledNote { Tone = NoteTone.Cozy, Text = "Saved you the sunny corner of the pond.\nIt has your name on it.\nAlmost." },
            new BundledNote { Tone = NoteTone.Cozy, Text = "Side-by-side naps are the best naps.\nThis is science.\nHonk." },
            new BundledNote { Tone = NoteTone.Cozy, Text = "I like the way you ripple.\nNo notes.\nJust honks." },
            new BundledNote { Tone = NoteTone.Cozy, Text = "You chased that cursor beautifully today.\nI honked a little with pride." },
            new BundledNote { Tone = NoteTone.Affectionate, Text = "If I found two peas, I would give you both.\nThat is how it is now." },
            new BundledNote { Tone = NoteTone.Affectionate, Text = "You are my favorite distraction.\nEvery prank can wait." },
            new BundledNote { Tone = NoteTone.Affectionate, Text = "I practiced my softest honk for you.\nHonk. (That was the soft one.)" },
            new BundledNote { Tone = NoteTone.Romantic, Text = "Under this same screen-light,\nI would swim beside you forever.\nHonk, my love." },
            new BundledNote { Tone = NoteTone.Romantic, Text = "Two geese, one desktop,\nzero regrets.\nYours, in feathers." },
            new BundledNote { Tone = NoteTone.Romantic, Text = "If honks were ripples, mine would all reach you." }
        };

        public static bool IsTextOnly(string note)
        {
            return note != null && note.Contains(TextOnlyMarker);
        }

        // Warmer bonds make affectionate and romantic notes more likely:
        // romantic weight is zero for a new pair and never dominates, so a
        // romantic delivery is possible at full warmth but never guaranteed.
        public static string ChoosePartnerNote(Random random, float bond)
        {
            double cozy = 1, affectionate = .25 + .75 * bond, romantic = bond * bond, roll = random.NextDouble() * (cozy + affectionate + romantic);
            NoteTone tone = roll < cozy ? NoteTone.Cozy : roll < cozy + affectionate ? NoteTone.Affectionate : NoteTone.Romantic;
            int match = 0, picked = 0;
            for (int i = 0; i < PartnerNotes.Length; i++)
                if (PartnerNotes[i].Tone == tone && random.Next(++match) == 0)
                    picked = i;
            for (int i = 0; i < PartnerNotes.Length && match == 0; i++)
                if (random.Next(i + 1) == 0)
                    picked = i;
            return PartnerNotes[picked].Text;
        }
    }
}
