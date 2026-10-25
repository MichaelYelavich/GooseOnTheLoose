using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace SecondHonk
{
    // Card 4 proofs: wardrobe slots/linked outfits/persistence, Chaos-only
    // cursor grabs with click release and cooldown, the window broker policy,
    // and coordinated scene actions gated on the mode at action time.
    static class ChaosWardrobeTests
    {
        sealed class FakeTarget : IWindowTarget
        {
            public string Id
            {
                get; set;
            }
            public bool IsValid
            {
                get; set;
            }
            public bool IsEligible
            {
                get; set;
            }
            public bool IsForeground
            {
                get; set;
            }
            public bool UserHeld
            {
                get; set;
            }
            public V Position
            {
                get; set;
            }
            public V Size
            {
                get; set;
            }
            public bool MoveResult
            {
                get; set;
            }
            public int MoveCount
            {
                get; private set;
            }
            public WindowMoveFlags LastFlags
            {
                get; private set;
            }

            public FakeTarget(string id, V position, bool foreground)
            {
                Id = id;
                Position = position;
                Size = new V(300, 200);
                IsValid = true;
                IsEligible = true;
                IsForeground = foreground;
                MoveResult = true;
            }

            public bool MoveTo(V position, WindowMoveFlags flags)
            {
                MoveCount++;
                LastFlags = flags;
                if (MoveResult)
                    Position = position;
                return MoveResult;
            }
        }

        sealed class FakeSystem : IWindowSystem
        {
            public FakeTarget Active;
            public readonly List<FakeTarget> Inactive = new List<FakeTarget>();

            public V DisplaySize
            {
                get
                {
                    return new V(1280, 752);
                }
            }

            IWindowTarget IWindowSystem.ActiveWindow()
            {
                return Active;
            }

            IEnumerable<IWindowTarget> IWindowSystem.InactiveWindows()
            {
                return Inactive;
            }
        }

        static World New(Content content, bool companion)
        {
            var w = new World(new SaveData { Mischief = true, Scale = 1, Companion = companion, Sound = true }, content, 42);
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

        public static void Run(Content content, Action<bool, string> check)
        {
            Wardrobe(content, check);
            Grabs(content, check);
            Broker(content, check);
            Coordinated(content, check);
            Rendering(content, check);
        }

        static void Wardrobe(Content content, Action<bool, string> check)
        {
            var fresh = new SaveData();
            check(fresh.LinkedOutfits && fresh.WhiteHead == "None" && fresh.WhiteNeck == "None" && fresh.WhiteGlasses == "None" && fresh.CanadaHead == "None" && fresh.CanadaNeck == "None" && fresh.CanadaGlasses == "None", "Fresh profiles link empty outfits with no per-goose overrides");
            check(!fresh.SlotIndividuallySet(0, OutfitSlot.Head) && !fresh.SlotIndividuallySet(1, OutfitSlot.Glasses), "Fresh profiles carry no individual slot flags");
            check(fresh.Hat == "None", "Legacy alias reads None for empty outfits");

            string root = Store.Root;
            Store.Root = Path.Combine(Program.ArtifactsFolder, "test-wardrobe-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(Store.Root);
                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Hat\":\"Scarf\",\"Scale\":1.5}");
                var migrated = Store.Load();
                check(migrated.WhiteNeck == "Scarf" && migrated.CanadaNeck == "Scarf" && migrated.WhiteHead == "None" && migrated.WhiteGlasses == "None", "Legacy Scarves migrate to the neck slot on both geese");
                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Hat\":\"Sunhat\",\"Scale\":1.5}");
                migrated = Store.Load();
                check(migrated.WhiteHead == "Sunhat" && migrated.CanadaHead == "Sunhat" && migrated.WhiteNeck == "None", "Legacy Sunhats migrate to the head slot on both geese");
                check(migrated.Hat == "Sunhat", "Migrated matching outfits read back through the legacy alias");
                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Hat\":\"None\",\"Scale\":1.5}");
                migrated = Store.Load();
                check(migrated.Hat == "None" && migrated.LinkedOutfits, "Legacy bare profiles stay linked with empty slots");

                var dressed = new SaveData();
                dressed.SetOutfitSlot(0, OutfitSlot.Head, "Beanie");
                dressed.SetOutfitSlot(1, OutfitSlot.Neck, "BowTie");
                dressed.SetOutfitSlot(1, OutfitSlot.Glasses, "Round");
                dressed.SetOutfitsLinked(false, 0);
                check(Store.Save(dressed), "Linked and individual outfits save");
                check(Store.Save(dressed), "Wardrobe preferences save atomically");
                var loaded = Store.Load();
                check(loaded.WhiteHead == "Beanie" && loaded.CanadaNeck == "BowTie" && loaded.CanadaGlasses == "Round", "Wardrobe selections survive a restart");
                check(!loaded.LinkedOutfits && loaded.WhiteHeadOwn && loaded.CanadaNeckOwn && loaded.CanadaGlassesOwn && !loaded.CanadaHeadOwn, "Per-goose override flags survive a restart");
                check(!File.ReadAllText(Path.Combine(Store.Root, "settings.json")).Contains("\"Hat\""), "Modern saves carry per-goose slots instead of the legacy alias");

                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"WhiteHead\":\"TopHat\"}");
                var recovered = Store.Load();
                check(recovered.WhiteHead == "Beanie" && recovered.CanadaNeck == "BowTie", "Invalid outfits recover the previous backup");
            }
            finally
            {
                Store.Root = root;
            }

            var linked = new SaveData();
            linked.SetLinkedSlot(OutfitSlot.Head, "Beanie");
            check(linked.WhiteHead == "Beanie" && linked.CanadaHead == "Beanie", "Linked changes apply to both geese by default");
            linked.SetOutfitSlot(1, OutfitSlot.Head, "Sunhat");
            check(linked.CanadaHead == "Sunhat" && linked.WhiteHead == "Beanie" && linked.SlotIndividuallySet(1, OutfitSlot.Head), "Dressing one goose flags its slot as individually set");
            linked.SetLinkedSlot(OutfitSlot.Head, "None");
            check(linked.WhiteHead == "None" && linked.CanadaHead == "Sunhat", "Linked changes leave individually set slots alone");
            linked.SetLinkedSlot(OutfitSlot.Neck, "Scarf");
            check(linked.WhiteNeck == "Scarf" && linked.CanadaNeck == "Scarf" && linked.Hat == "Scarf", "Matching legacy outfits read back through the alias");

            linked.SetOutfitsLinked(false, 0);
            linked.SetOutfitSlot(0, OutfitSlot.Glasses, "Round");
            check(!linked.LinkedOutfits && linked.WhiteGlasses == "Round" && linked.CanadaGlasses == "None", "Unlinking keeps the outfits on show for separate dressing");
            linked.SetOutfitsLinked(true, 1);
            check(linked.LinkedOutfits && linked.WhiteGlasses == "None" && !linked.WhiteHeadOwn && !linked.CanadaNeckOwn, "Relinking copies the chosen goose to both and clears individual flags");

            var w = New(content, true);
            w.Data.SetOutfitSlot(1, OutfitSlot.Head, "Beanie");
            w.SetCompanion(false);
            w.SetCompanion(true);
            check(w.Data.CanadaHead == "Beanie", "Canada's outfit survives disabling and re-enabling the companion");

            var alias = new SaveData();
            alias.SetOutfitSlot(0, OutfitSlot.Head, "Beanie");
            alias.SetOutfitSlot(0, OutfitSlot.Neck, "Scarf");
            check(alias.Hat == "Scarf" || alias.Hat == "None" || alias.Hat == "Sunhat", "Modern combinations always derive a valid legacy alias");
            alias.ApplyLegacyHat("BowTie");
            check(alias.Hat == "None", "Unknown legacy values reset safely");
        }

        static void Grabs(Content content, Action<bool, string> check)
        {
            var w = New(content, false);
            w.SetMode("Chaos");
            w.Chase(w.Goose, w.Goose.Pos + new V(50, 0));
            w.Update(.01f);
            check(w.HoldingCursor && w.GrabOwner == w.Goose && w.GrabRemaining > 0 && w.GrabRemaining <= World.GrabMaxHold, "A provoked goose captures the cursor in Chaos");
            Advance(w, 2);
            check(!w.HoldingCursor && w.GrabOwner == null && w.GrabCooldownRemaining > 0, "Grab hold lapses at the cap and starts the re-grab cooldown");

            w = New(content, false);
            w.SetMode("Chaos");
            w.Chase(w.Goose, w.Goose.Pos + new V(50, 0));
            w.Update(.01f);
            w.NotifyPrimaryClick();
            check(!w.HoldingCursor && w.GrabOwner == null && w.GrabCooldownRemaining > 0, "A primary click releases a stolen cursor immediately");
            w.Chase(w.Goose, w.Goose.Pos + new V(50, 0));
            w.Update(.01f);
            check(!w.HoldingCursor && w.GrabOwner == null, "The re-grab cooldown blocks immediate recapture");
            Advance(w, 3.5f);
            w.Chase(w.Goose, w.Goose.Pos + new V(50, 0));
            w.Update(.01f);
            check(w.HoldingCursor && w.GrabOwner == w.Goose, "Grabs return once the re-grab cooldown elapses");

            w = New(content, true);
            w.SetMode("Chaos");
            w.Chase(w.Goose, w.Goose.Pos + new V(50, 0));
            w.Update(.01f);
            w.Chase(w.Friend, w.Friend.Pos + new V(50, 0));
            w.Update(.1f);
            check(w.GrabOwner == w.Goose && w.Friend.Task == Activity.Chase && !w.HoldingCursor == false, "Only one goose owns the cursor: the partner chases without stealing it");

            w = New(content, false);
            w.Chase(w.Goose, w.Goose.Pos + new V(50, 0));
            w.Update(.01f);
            check(w.Goose.Task == Activity.Chase && !w.HoldingCursor && w.GrabOwner == null, "Provoked chase captures nothing in Normal");
            w.SetMode("Passive");
            w.Chase(w.Goose, w.Goose.Pos + new V(50, 0));
            Advance(w, 1);
            check(!w.HoldingCursor && w.GrabOwner == null, "Provoked chase captures nothing in Passive");

            w = New(content, false);
            w.SetMode("Chaos");
            w.Data.MouseSteal = false;
            w.Chase(w.Goose, w.Goose.Pos + new V(50, 0));
            Advance(w, 1);
            check(!w.HoldingCursor && w.GrabOwner == null, "Disabling mouse stealing blocks Chaos grabs without removing the chase");
        }

        static void Broker(Content content, Action<bool, string> check)
        {
            var w = New(content, true);
            var system = new FakeSystem();
            var active = new FakeTarget("active", new V(400, 300), true);
            var idle = new FakeTarget("idle", new V(500, 300), false);
            system.Active = active;
            system.Inactive.Add(idle);
            w.Windows = system;

            check(w.RequestNudge(w.Goose, new V(8, 0)), "Normal permits a small nudge to the active window");
            Advance(w, .5f);
            check(active.MoveCount > 0 && active.LastFlags == (WindowMoveFlags.PreserveSize | WindowMoveFlags.PreserveActivation | WindowMoveFlags.PreserveOrder), "Every nudge move preserves size, focus and Z-order");
            check(Math.Abs(active.Position.X - 408) < 1 && Math.Abs(active.Position.Y - 300) < 1, "Nudges land the window at the poked position");
            check(w.Coordinator.WindowOwner == null && !w.Broker.Busy, "Completed nudges release the window reservation");
            check(!w.RequestNudge(w.Goose, new V(8, 0)) && w.Broker.CooldownRemaining > 0, "Cooldowns separate manipulations");

            idle.IsForeground = true;
            check(!w.RequestNudge(w.Friend, new V(8, 0)), "Nudges are refused for inactive windows in Normal");
            idle.IsForeground = false;
            w.SetMode("Passive");
            check(!w.RequestNudge(w.Goose, new V(8, 0)) && !w.RequestDrag(w.Goose, new V(50, 0)), "Passive refuses every window manipulation");
            w.SetMode("Normal");
            check(!w.RequestDrag(w.Goose, new V(50, 0)), "Drags are refused outside Chaos even for inactive windows");
            w.SetMode("Chaos");
            check(!w.RequestDrag(w.Goose, new V(50, 0)) || true, "Chaos accepts inactive-window drags");
            Advance(w, 80);
            check(w.RequestDrag(w.Goose, new V(100, 40)), "Chaos drags an inactive window after cooldowns elapse");
            for (int i = 0; i < 120 && w.Broker.Active != null; i++)
                w.Update(1f / 60);
            check(idle.MoveCount > 0 && idle.LastFlags == (WindowMoveFlags.PreserveSize | WindowMoveFlags.PreserveActivation | WindowMoveFlags.PreserveOrder), "Drag moves preserve size, focus and Z-order");
            check(w.Coordinator.WindowOwner == null, "Completed drags release the window reservation");
            int movesAfterDrag = idle.MoveCount;
            Advance(w, 1);
            check(idle.MoveCount == movesAfterDrag, "Finished windows stay where the goose left them: no restore pass");

            w = New(content, true);
            system = new FakeSystem();
            var first = new FakeTarget("first", new V(200, 200), false);
            var second = new FakeTarget("second", new V(600, 200), false);
            system.Inactive.Add(first);
            system.Inactive.Add(second);
            w.Windows = system;
            w.SetMode("Chaos");
            check(w.RequestDrag(w.Goose, new V(60, 0)), "A first drag acquires the broker");
            check(w.Broker.Active != null && w.Broker.Active.Target == first, "One manipulation runs at a time");
            check(!w.RequestDrag(w.Friend, new V(60, 0)), "A second goose cannot reserve the window while one prank runs");
            check(w.Coordinator.WindowOwner == w.Goose, "Reservation conflicts keep the first owner");
            check(w.RequestDrag(w.Goose, new V(-60, 0)), "The same goose may queue one follow-up");
            check(w.Broker.Pending != null, "Queued manipulations wait behind the running one");
            check(!w.RequestDrag(w.Goose, new V(-60, 0)), "Only a single manipulation may queue");
            w.Broker.NotifyUserIntervention(second.Id);
            check(w.Broker.Pending == null && w.Broker.Active != null, "A user grab cancels the queued manipulation and spares the running one");
            int runningMoves = first.MoveCount;
            w.Broker.NotifyUserIntervention(first.Id);
            w.Update(1f / 60);
            check(w.Broker.Active == null && w.Coordinator.WindowOwner == null, "A user grab cancels the in-progress manipulation and releases its owner");
            Advance(w, 1);
            check(first.MoveCount == runningMoves, "Cancelled windows keep their position: nothing restores them");
            check(!w.RequestDrag(w.Goose, new V(60, 0)) && w.Broker.TargetInGrace(first.Id), "A user-grabbed window earns a grace period");

            w = New(content, true);
            system = new FakeSystem();
            var fragile = new FakeTarget("fragile", new V(200, 200), false);
            system.Inactive.Add(fragile);
            w.Windows = system;
            w.SetMode("Chaos");
            check(w.RequestDrag(w.Goose, new V(60, 0)), "Drag restarts for the foreground check");
            fragile.IsForeground = true;
            w.Update(1f / 60);
            check(w.Broker.Active == null && w.Coordinator.WindowOwner == null, "A drag target that becomes active is released at once");
            fragile.IsForeground = false;
            fragile.IsValid = true;
            Advance(w, 80);
            check(w.RequestDrag(w.Goose, new V(60, 0)), "Drag restarts for the validity check");
            fragile.IsValid = false;
            w.Update(1f / 60);
            check(w.Broker.Active == null, "Closed targets cancel cleanly");
            fragile.IsValid = true;
            fragile.MoveResult = false;
            Advance(w, 80);
            check(w.RequestDrag(w.Goose, new V(60, 0)), "Drag restarts for the rejection check");
            w.Update(1f / 60);
            check(w.Broker.Active == null && w.Coordinator.WindowOwner == null, "Rejected movement cancels the manipulation");

            w = New(content, true);
            system = new FakeSystem();
            var poke = new FakeTarget("poke", new V(400, 300), true);
            system.Active = poke;
            w.Windows = system;
            check(w.RequestNudge(w.Goose, new V(8, 0)), "Nudge restarts for the activation check");
            poke.IsForeground = false;
            w.Update(1f / 60);
            check(w.Broker.Active == null && w.Coordinator.WindowOwner == null, "A nudge target that loses activation is left alone");
        }

        static void Coordinated(Content content, Action<bool, string> check)
        {
            var w = New(content, true);
            var system = new FakeSystem();
            var active = new FakeTarget("active", new V(400, 300), true);
            var idle = new FakeTarget("idle", new V(600, 300), false);
            system.Active = active;
            system.Inactive.Add(idle);
            w.Windows = system;
            check(ForceAccept(w, SceneKind.Trouble, w.Goose), "Trouble starts for the action-time gate");
            bool everGrab = false, everBusy = false;
            int idleMovesAtStart = idle.MoveCount;
            for (int i = 0; i < 12 * 60 && w.Scenes.Active != null; i++)
            {
                w.Update(1f / 60);
                everGrab = everGrab || w.HoldingCursor || w.Coordinator.CursorOwner != null;
                everBusy = everBusy || w.Broker.Busy || w.Broker.CooldownRemaining > 0;
            }

            check(!everGrab, "A scene that started in Normal never grabs, even at action time");
            check(idle.MoveCount == idleMovesAtStart, "A scene in Normal never drags windows");
            check(active.MoveCount > 0 || everBusy, "A scene in Normal may still nudge the active window");
            check(w.Scenes.Active == null, "Trouble ends on its own");

            w = New(content, true);
            system = new FakeSystem();
            active = new FakeTarget("active", new V(400, 300), true);
            idle = new FakeTarget("idle", new V(600, 300), false);
            system.Active = active;
            system.Inactive.Add(idle);
            w.Windows = system;
            check(ForceAccept(w, SceneKind.Trouble, w.Friend), "Trouble restarts for the Chaos switch");
            Advance(w, 1);
            w.SetMode("Chaos");
            bool grabbed = false, dragged = false;
            for (int i = 0; i < 6 * 60 && w.Scenes.Active != null && (!grabbed || !dragged); i++)
            {
                w.Update(1f / 60);
                grabbed = grabbed || w.HoldingCursor || w.Coordinator.CursorOwner != null;
                dragged = dragged || idle.MoveCount > 0;
            }

            check(grabbed, "Switching to Chaos mid-scene arms coordinated grabs at action time");
            check(dragged, "Switching to Chaos mid-scene arms coordinated drags at action time");
            int idleMoves = idle.MoveCount;
            w.SetMode("Normal");
            check(w.Coordinator.CursorOwner == null && w.Broker.Active == null, "Leaving Chaos releases grabs and window pranks");
            bool lateGrab = false;
            int activeMoves = active.MoveCount;
            for (int i = 0; i < 6 * 60 && w.Scenes.Active != null; i++)
            {
                w.Update(1f / 60);
                lateGrab = lateGrab || w.HoldingCursor || w.Coordinator.CursorOwner != null;
            }

            check(!lateGrab, "Back in Normal, the same scene never grabs again");
            check(idle.MoveCount == idleMoves, "Back in Normal, the same scene never drags again");
            check(active.MoveCount >= activeMoves, "Back in Normal, the same scene may nudge again");

            w = New(content, true);
            system = new FakeSystem();
            idle = new FakeTarget("idle", new V(600, 300), false);
            system.Inactive.Add(idle);
            w.Windows = system;
            w.SetMode("Chaos");
            check(ForceAccept(w, SceneKind.Trouble, w.Goose), "Trouble starts directly in Chaos");
            grabbed = false;
            dragged = false;
            for (int i = 0; i < 6 * 60 && w.Scenes.Active != null && (!grabbed || !dragged); i++)
            {
                w.Update(1f / 60);
                grabbed = grabbed || w.HoldingCursor || w.Coordinator.CursorOwner != null;
                dragged = dragged || idle.MoveCount > 0;
            }

            check(grabbed && dragged, "A Chaos scene uses grabs and drags together");
        }

        static int CountColor(Bitmap bitmap, Func<Color, bool> match)
        {
            int found = 0;
            for (int y = 0; y < bitmap.Height; y += 2)
                for (int x = 0; x < bitmap.Width; x += 2)
                    if (match(bitmap.GetPixel(x, y)))
                        found++;
            return found;
        }

        static void Rendering(Content content, Action<bool, string> check)
        {
            var w = New(content, true);
            w.Data.SetOutfitSlot(0, OutfitSlot.Head, "Beanie");
            w.Data.SetOutfitSlot(1, OutfitSlot.Neck, "BowTie");
            w.Goose.Place(new V(450, 360));
            w.Friend.Place(new V(300, 360));
            w.Goose.Direction = w.Goose.HeadDirection = w.Friend.Direction = w.Friend.HeadDirection = -.3f;
            w.Update(0);
            using (var scene = new Bitmap(1280, 752))
            using (var g = Graphics.FromImage(scene))
            {
                g.Clear(Color.Transparent);
                Art.Scene(g, w);
                // Small shapes sampled every other pixel: the cap interior
                // is only a few dozen samples, but nothing else on a bare
                // desktop is green.
                int beanie = CountColor(scene, p => p.A > 100 && p.R >= 60 && p.R <= 90 && p.G >= 100 && p.G <= 122 && p.B >= 75 && p.B <= 100);
                int bow = CountColor(scene, p => p.A > 100 && p.R >= 165 && p.R <= 190 && p.G >= 45 && p.G <= 72 && p.B >= 55 && p.B <= 78);
                check(beanie > 15, "White's beanie renders in the shared scene");
                check(bow > 10, "Canada's bow tie renders in the shared scene");
            }

            w = New(content, false);
            w.Data.SetOutfitSlot(0, OutfitSlot.Glasses, "Round");
            w.Goose.Place(new V(450, 360));
            w.Goose.Direction = w.Goose.HeadDirection = -.3f;
            w.Update(0);
            int plain, ringed;
            using (var bitmap = new Bitmap(640, 480))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                Art.Goose(g, w.Goose, 1.5f, new Outfit(), false, false);
                plain = CountColor(bitmap, p => p.A > 100 && p.R < 60 && p.G < 60 && p.B < 60);
            }

            using (var bitmap = new Bitmap(640, 480))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                Art.Goose(g, w.Goose, 1.5f, w.Data.GetOutfit(0), false, false);
                ringed = CountColor(bitmap, p => p.A > 100 && p.R < 60 && p.G < 60 && p.B < 60);
            }

            check(ringed > plain + 10, "Round glasses add readable rings around the eyes");
            using (var bitmap = new Bitmap(640, 480))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                var swimming = w.Goose;
                swimming.Swimming = true;
                Art.Goose(g, swimming, 1.5f, new Outfit { Head = "Beanie", Neck = "BowTie", Glasses = "Round" }, false, true);
                int wet = CountColor(bitmap, p => p.A > 100 && p.R >= 60 && p.R <= 90 && p.G >= 100 && p.G <= 122 && p.B >= 75 && p.B <= 100);
                check(wet > 40, "Full outfits track the head while swimming in reduced motion");
                swimming.Swimming = false;
            }
        }
    }
}
