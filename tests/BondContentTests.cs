using System;
using System.IO;
using System.Linq;

namespace SecondHonk
{
    static class BondContentTests
    {
        static World New(Content content, bool companion, bool mischief, int seed)
        {
            var w = new World(new SaveData { Mischief = mischief, Scale = 1, Companion = companion, Sound = true }, content, seed);
            w.Resize(1280, 752);
            w.Goose.Place(new V(450, 360));
            if (companion)
                w.Friend.Place(new V(300, 360));
            w.Update(0);
            return w;
        }

        static void Advance(World w, float seconds)
        {
            for (int i = 0; i < seconds * 60; i++)
                w.Update(1f / 60);
        }

        static bool ForceAccept(World w, SceneKind kind, Goose leader)
        {
            w.Goose.SocialCooldown = w.Friend.SocialCooldown = 0;
            if (!w.StartScene(kind, leader))
                return false;
            for (int i = 0; i < 200 && w.Scenes.Active != null && w.Scenes.Active.Phase != ScenePhase.Activity; i++)
            {
                w.Update(1f / 60);
                var active = w.Scenes.Active;
                if (active != null && active.Phase == ScenePhase.Invite && active.Invite != null && active.Invite.State == InvitationState.Offered)
                    w.Coordinator.Respond(active.Invite.Id, true);
            }

            return w.Scenes.Active != null && w.Scenes.Active.Phase == ScenePhase.Activity;
        }

        static void RunToEnd(World w, float capSeconds)
        {
            for (int i = 0; i < capSeconds * 60 && w.Scenes.Active != null; i++)
                w.Update(1f / 60);
        }

        static void CompleteScene(World w, SceneKind kind)
        {
            if (!ForceAccept(w, kind, w.Goose))
                throw new Exception("Could not start " + kind);
            RunToEnd(w, 90);
            if (w.Scenes.Active != null)
                throw new Exception(kind + " did not finish");
        }

        static int CompleteDelivery(World w, Goose actor)
        {
            int before = w.Deliveries;
            for (int i = 0; i < 40 * 60 && w.Deliveries == before; i++)
                w.Update(1f / 60);
            return w.Deliveries - before;
        }

        public static void Run(Content content, Action<bool, string> check)
        {
            // Saved bond basics: compatible default, validation, recovery.
            var fresh = new SaveData();
            check(fresh.Bond == 0, "Fresh profiles start with no bond");
            string root = Store.Root;
            Store.Root = Path.Combine(Program.ArtifactsFolder, "test-bond-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Store.Root);
                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Hat\":\"Scarf\",\"Scale\":1.5}");
                check(Store.Load().Bond == 0, "Upgraded profiles without a bond field default to no bond");
                check(Store.Save(new SaveData { Hat = "Scarf", Bond = .25f }), "Bonded preferences save");
                check(Store.Save(new SaveData { Hat = "Scarf", Bond = .25f }), "Bonded preferences save atomically");
                check(Math.Abs(Store.Load().Bond - .25f) < 1e-6, "The saved bond survives a restart");
                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Bond\":7}");
                check(Math.Abs(Store.Load().Bond - .25f) < 1e-6, "Out-of-range bonds recover the previous backup");
                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Bond\":\"warm\"}");
                check(Math.Abs(Store.Load().Bond - .25f) < 1e-6, "Unparsable bonds recover the previous backup");
                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Mode\":\"Loud\",\"Bond\":.5f}");
                var recovered = Store.Load();
                check(recovered.Mode == "Normal" && Math.Abs(recovered.Bond - .25f) < 1e-6, "Invalid modes still recover the previous bonded backup");
            }
            finally
            {
                Store.Root = root;
            }

            // Accrual only through varied shared scenes during unpaused pair play.
            var w = New(content, true, false, 42);
            CompleteScene(w, SceneKind.Rest);
            check(Math.Abs(w.Data.Bond - .0008f) < 1e-9, "One varied shared scene earns the full bond credit");
            CompleteScene(w, SceneKind.Rest);
            check(Math.Abs(w.Data.Bond - (.0008f + .00012f)) < 1e-9, "Repeating the same scene inside the cooldown earns only the repeat credit");
            CompleteScene(w, SceneKind.Tug);
            check(Math.Abs(w.Data.Bond - (2 * .0008f + .00012f)) < 1e-9, "A different shared scene earns the full credit again");
            check(w.Data.Bond <= 1, "The bond never exceeds its cap");

            w = New(content, true, false, 43);
            for (int i = 0; i < 4; i++)
                CompleteScene(w, SceneKind.Rest);
            float repeated = w.Data.Bond;
            w = New(content, true, false, 43);
            CompleteScene(w, SceneKind.Rest);
            CompleteScene(w, SceneKind.BallPass);
            CompleteScene(w, SceneKind.FetchRelay);
            CompleteScene(w, SceneKind.Tug);
            check(w.Data.Bond > repeated * 2, "Varied shared experiences grow the bond far faster than repetition");

            // No accrual while paused, solo, or from the clock.
            w = New(content, true, false, 44);
            w.SetPaused(true);
            Advance(w, 120);
            check(w.Data.Bond == 0 && w.Time == 0, "Paused simulation time contributes nothing to the bond");
            w = New(content, false, true, 44);
            w.Data.RandomAttacks = true;
            Advance(w, 300);
            check(w.Data.Bond == 0, "Solo play grows no bond");
            w = New(content, true, false, 44);
            w.CreditBond(SceneKind.Rest);
            float pausedBefore = w.Data.Bond;
            w.SetPaused(true);
            w.CreditBond(SceneKind.Rest);
            check(w.Data.Bond == pausedBefore, "Scene credit is refused while paused");
            w.SetPaused(false);
            w.SetCompanion(false);
            w.CreditBond(SceneKind.Rest);
            check(w.Data.Bond == pausedBefore, "Scene credit is refused without both geese");
            var a = New(content, true, true, 45);
            var b = New(content, true, true, 45);
            Advance(a, 120);
            Advance(b, 120);
            check(Math.Abs(a.Data.Bond - b.Data.Bond) < 1e-9, "Identical play histories produce identical bonds: no hidden clock input");

            // The bond survives Canada being disabled and re-enabled.
            w = New(content, true, false, 46);
            CompleteScene(w, SceneKind.Rest);
            float grown = w.Data.Bond;
            check(grown > 0, "Shared play grew the bond before the toggle");
            w.SetCompanion(false);
            check(Math.Abs(w.Data.Bond - grown) < 1e-9, "Disabling Canada never erases the bond");
            Advance(w, 60);
            check(Math.Abs(w.Data.Bond - grown) < 1e-9, "Solo time after disabling Canada adds nothing");
            w.SetCompanion(true);
            check(Math.Abs(w.Data.Bond - grown) < 1e-9, "Re-enabling Canada restores pair play with the bond intact");

            // Pacing: slow growth that projects into the 20 to 30 hour band.
            w = New(content, true, true, 47);
            Advance(w, 1800);
            check(w.Scenes.CompletedScenes > 0, "Accelerated pair play completes shared scenes");
            check(w.Data.Bond > 0 && w.Data.Bond < .05f, "Half an hour of pair play warms the bond only slightly");
            double perHour = w.Data.Bond * 2;
            double hoursToHalf = .5 / Math.Max(1e-9, perHour);
            check(hoursToHalf >= 15 && hoursToHalf <= 60, "Pacing evidence: " + w.Scenes.CompletedScenes + " scenes and bond " + w.Data.Bond.ToString("F5") + " in 30 sim-minutes project to " + hoursToHalf.ToString("F1") + " hours for a half-warm bond");

            // Warmth influences invitations: same seed, only the bond differs.
            int acceptsAt = 0, tries = 0;
            for (int bond = 0; bond <= 1; bond++)
            {
                var v = New(content, true, false, 48);
                v.Data.Bond = bond;
                for (int i = 0; i < 120; i++)
                {
                    v.Goose.SocialCooldown = v.Friend.SocialCooldown = 0;
                    if (!v.StartScene(SceneKind.Rest, v.Goose))
                        continue;
                    tries++;
                    for (int j = 0; j < 400 && v.Scenes.Active != null && (v.Scenes.Active.Phase == ScenePhase.Notice || v.Scenes.Active.Phase == ScenePhase.Invite); j++)
                        v.Update(1f / 60);
                    var active = v.Scenes.Active;
                    if (active != null && active.Phase == ScenePhase.Activity)
                        acceptsAt += bond == 1 ? 1 : -1;
                    v.Scenes.Cancel(v);
                }
            }

            check(acceptsAt > 0, "A warm bond earns more accepted invitations than a new one");

            // Warmth influences comfortable proximity.
            var cold = New(content, true, false, 49);
            ForceAccept(cold, SceneKind.Rest, cold.Goose);
            float coldGap = V.Distance(cold.Scenes.Active.SpotA, cold.Scenes.Active.SpotB);
            var warm = New(content, true, false, 49);
            warm.Data.Bond = 1;
            ForceAccept(warm, SceneKind.Rest, warm.Goose);
            float warmGap = V.Distance(warm.Scenes.Active.SpotA, warm.Scenes.Active.SpotB);
            check(warmGap < coldGap && warmGap > 12 * warm.Scale, "Warmer pairs rest a little closer, never on top of each other");

            // Recipient-aware delivery: solo and explicit play stay user-facing.
            w = New(content, false, false, 50);
            w.Content.Notes.Clear();
            w.Content.Notes.Add("USER-SENTINEL");
            int soloMisses = 0, soloImageMisses = 0;
            for (int i = 0; i < 6; i++)
            {
                bool wantImage = i % 2 == 0;
                w.AutoDeliver(w.Goose, wantImage);
                if (w.Goose.DeliveryRecipient != DeliveryRecipient.User || w.Goose.DeliveryNote != "USER-SENTINEL")
                    soloMisses++;
                if ((w.Goose.DeliveryImagePath == "") != (!wantImage || w.Content.Images.Count == 0))
                    soloImageMisses++;
                w.CancelActor(w.Goose);
            }

            check(soloMisses == 0, "Solo autonomy delivers only the user's own content");
            check(soloImageMisses == 0, "Solo image requests keep the safe image handling");

            w = New(content, true, false, 51);
            w.Content.Notes.Clear();
            w.Content.Notes.Add("USER-SENTINEL");
            w.Content.Images.Add("user-pic.png");
            int explicitMisses = 0;
            for (int i = 0; i < 6; i++)
            {
                w.Deliver(i % 2 == 0 ? w.Goose : w.Friend, true);
                Goose actor = w.Coordinator.DeliveryOwner;
                if (actor.DeliveryRecipient != DeliveryRecipient.User || actor.DeliveryNote != "USER-SENTINEL")
                    explicitMisses++;
                w.CancelActor(actor);
            }

            check(explicitMisses == 0, "Explicit requests stay user-facing even in pair play");

            // Affection follows warmth: seeded draws shift tone but never promise romance.
            const int draws = 200;
            int romanticCold = 0;
            var chill = new Random(52);
            for (int i = 0; i < draws; i++)
            {
                string note = BundledContent.ChoosePartnerNote(chill, 0);
                if (BundledContent.PartnerNotes.First(n => n.Text == note).Tone == NoteTone.Romantic)
                    romanticCold++;
            }

            var warmRandom = new Random(52);
            int romanticWarm = 0, cozyWarm = 0, catalogMisses = 0, userLeaks = 0;

            for (int i = 0; i < draws; i++)
            {
                string note = BundledContent.ChoosePartnerNote(warmRandom, 1);
                var tone = BundledContent.PartnerNotes.First(n => n.Text == note).Tone;
                if (tone == NoteTone.Romantic)
                    romanticWarm++;
                if (tone == NoteTone.Cozy)
                    cozyWarm++;
                if (!BundledContent.PartnerNotes.Any(n => n.Text == note))
                    catalogMisses++;
                if (note.Contains("USER-SENTINEL"))
                    userLeaks++;
            }

            check(catalogMisses == 0, "Partner payloads come only from the bundled catalog");
            check(userLeaks == 0, "Partner payloads never leak user content");
            check(romanticCold == 0, "New pairs never draw romantic notes");
            check(romanticWarm > 0 && cozyWarm > 0, "Full warmth makes romance likely but never guaranteed");

            // Warmer bonds make goose-to-goose delivery more likely, never certain.
            int partnersCold = 0, partnersWarm = 0, userNotesWarm = 0;
            var coldWorld = New(content, true, false, 53);
            coldWorld.Content.Notes.Clear();
            coldWorld.Content.Notes.Add("USER-SENTINEL");
            for (int i = 0; i < 40; i++)
            {
                coldWorld.AutoDeliver(coldWorld.Goose, false);
                if (coldWorld.Goose.DeliveryRecipient == DeliveryRecipient.Partner)
                    partnersCold++;
                coldWorld.CancelActor(coldWorld.Goose);
            }

            var warmWorld = New(content, true, false, 53);
            warmWorld.Content.Notes.Clear();
            warmWorld.Content.Notes.Add("USER-SENTINEL");
            warmWorld.Data.Bond = 1;
            int toneMisses = 0;
            for (int i = 0; i < 40; i++)
            {
                warmWorld.AutoDeliver(warmWorld.Goose, false);
                if (warmWorld.Goose.DeliveryRecipient == DeliveryRecipient.Partner)
                {
                    partnersWarm++;
                    if (!BundledContent.PartnerNotes.Any(n => n.Text == warmWorld.Goose.DeliveryNote))
                        toneMisses++;
                }
                else
                {
                    userNotesWarm++;
                    if (warmWorld.Goose.DeliveryNote != "USER-SENTINEL")
                        toneMisses++;
                }

                warmWorld.CancelActor(warmWorld.Goose);
            }

            check(toneMisses == 0, "Autonomy delivers bundled tones to partners and user content to users");

            check(partnersWarm > partnersCold, "Warmer bonds send more goose-to-goose notes");
            check(partnersWarm < 40 && userNotesWarm > 0, "Even warm pairs still surprise the user: never guaranteed");
            check(warmWorld.Goose.DeliveryRecipient == DeliveryRecipient.User || partnersWarm > 0, "Recipient selection stays deterministic per seed");

            // THIS COULD HAVE BEEN A HONK stays text-only.
            w = New(content, false, false, 54);
            w.Content.Notes.Clear();
            w.Content.Notes.Add(BundledContent.TextOnlyMarker);
            w.Content.Images.Add("user-pic.png");
            w.Deliver(w.Goose, true);
            check(w.Goose.DeliveryNote.Contains(BundledContent.TextOnlyMarker) && w.Goose.DeliveryImagePath == "" && w.DeliveryImage == "", "The marker note never takes a picture payload");

            // Snapshot immunity: reloads and companion toggles cannot reroute.
            w = New(content, true, false, 55);
            w.Content.Notes.Clear();
            w.Content.Notes.Add("original payload");
            w.Deliver(w.Goose, false);
            string snapNote = w.Goose.DeliveryNote;
            var snapRecipient = w.Goose.DeliveryRecipient;
            Goose snapSender = w.Coordinator.DeliveryOwner;
            w.Content.Notes.Clear();
            w.Content.Notes.Add("replacement junk");
            w.Content.Images.Add("junk.png");
            w.SetCompanion(false);
            check(w.Goose.DeliveryNote == snapNote && w.Goose.DeliveryRecipient == snapRecipient && w.Coordinator.DeliveryOwner == snapSender, "Reloads and Canada toggles leave an in-flight snapshot untouched");
            check(CompleteDelivery(w, w.Goose) == 1 && w.Coordinator.DeliveryOwner == null, "White's delivery completes after Canada is disabled");
            check(w.DeliveryText == snapNote, "The delivered payload is the captured one, not the reloaded content");

            // A disabled companion finishes its own in-flight delivery intact.
            w = New(content, true, false, 56);
            w.Deliver(w.Friend, false);
            string friendNote = w.Friend.DeliveryNote;
            var friendRecipient = w.Friend.DeliveryRecipient;
            w.SetCompanion(false);
            check(w.Friend.Task == Activity.Deliver && w.Coordinator.DeliveryOwner == w.Friend, "Disabling Canada parks the actor but keeps its delivery");
            check(CompleteDelivery(w, w.Friend) == 1, "The companion's in-flight delivery still completes");
            check(w.DeliveryText == friendNote && w.Friend.Task == Activity.Wander && w.Coordinator.DeliveryOwner == null, "Payload, sender and recipient survive the toggle with a clean handoff");

            // Live settings reloads preserve the running bond and session truths.
            string liveRoot = Store.Root;
            Store.Root = Path.Combine(Program.ArtifactsFolder, "test-live-bond-" + Guid.NewGuid().ToString("N"));
            try
            {
                UserContent.Initialize(AppDomain.CurrentDomain.BaseDirectory, null, false);
                var live = New(new Content(Store.Root), true, false, 57);
                using (var monitor = new ContentMonitor(live))
                {
                    live.Data.Bond = .4f;
                    File.WriteAllText(Path.Combine(Store.Root, "Notes", "Fresh.txt"), "Fresh user note");
                    monitor.Reload();
                    check(Math.Abs(live.Data.Bond - .4f) < 1e-6, "Content reloads never overwrite the running bond");
                    check(live.Content.Notes.Contains("Fresh user note"), "User content still reloads live");
                    File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Bond\":9,\"Mode\":\"Loud\"}");
                    monitor.Reload();
                    check(Math.Abs(live.Data.Bond - .4f) < 1e-6 && live.Data.Mode == "Normal", "Invalid live bonds retain the last valid configuration");
                }
            }
            finally
            {
                Store.Root = liveRoot;
            }

            // Imported content stays user-facing by default.
            string importRoot = Store.Root;
            Store.Root = Path.Combine(Program.ArtifactsFolder, "test-import-" + Guid.NewGuid().ToString("N"));
            try
            {
                UserContent.Initialize(AppDomain.CurrentDomain.BaseDirectory, null, false);
                File.WriteAllText(Path.Combine(Store.Root, "Notes", "Diary.txt"), "My private diary page");
                var imported = new Content(Store.Root);
                check(imported.Notes.Contains("My private diary page"), "Imported notes load as user content");
                var importer = New(imported, true, false, 58);
                importer.Data.Bond = 1;
                bool leaked = false;
                for (int i = 0; i < 40; i++)
                {
                    importer.AutoDeliver(importer.Goose, false);
                    if (importer.Goose.DeliveryRecipient == DeliveryRecipient.Partner && importer.Goose.DeliveryNote.Contains("diary"))
                        leaked = true;
                    importer.CancelActor(importer.Goose);
                }

                check(!leaked, "Partner selection never guesses imported private text as its payload");
            }
            finally
            {
                Store.Root = importRoot;
            }

            // Partner deliveries can earn an answering honk; the pipeline stays serialized.
            w = New(content, true, false, 59);
            int honks = 0;
            w.Honked += actor => honks++;
            int partnerRuns = 0;
            for (int i = 0; i < 30 && partnerRuns < 6; i++)
            {
                w.AutoDeliver(w.Goose, false);
                if (w.Goose.DeliveryRecipient != DeliveryRecipient.Partner)
                {
                    w.CancelActor(w.Goose);
                    continue;
                }

                partnerRuns++;
                if (CompleteDelivery(w, w.Goose) != 1)
                    throw new Exception("Partner delivery did not complete");
            }

            check(partnerRuns > 0 && honks > 0, "Goose-to-goose deliveries complete and can earn an answering honk");
            check(w.Coordinator.DeliveryOwner == null && w.Coordinator.ReservationCount == 0, "Deliveries release the serialized pipeline");

            // The four-window cap and safe handling are unchanged.
            using (var session = new DesktopSession(new SaveData { Sound = false }, new Content(Store.Root), "", true))
            {
                for (int i = 0; i < 5; i++)
                {
                    session.World.DeliveryText = "Cap note " + i;
                    session.World.DeliveryImage = "";
                    session.BeginDelivery();
                    System.Windows.Forms.Application.DoEvents();
                }

                int open = System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().Count(f => f is GooseWindow);
                check(open == 4, "The four-window delivery cap still holds");
                session.ExitThread();
            }

            // Snapshot immunity reaches the native window with the captured payload.
            using (var session = new DesktopSession(new SaveData { Sound = false }, new Content(Store.Root), "", true))
            {
                session.World.Content.Notes.Clear();
                session.World.Content.Notes.Add("captured note");
                session.World.Deliver(false);
                session.World.Content.Notes.Clear();
                session.World.Content.Notes.Add("reloaded junk");
                session.BeginDelivery();
                System.Windows.Forms.Application.DoEvents();
                var shown = System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().OfType<GooseWindow>().FirstOrDefault();
                string text = shown != null ? shown.Controls.OfType<System.Windows.Forms.TextBox>().FirstOrDefault()?.Text : null;
                check(text != null && text.Contains("captured note"), "The native window shows the captured payload, not reloaded content");
                session.ExitThread();
            }
        }
    }
}
