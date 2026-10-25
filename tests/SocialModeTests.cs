using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace SecondHonk
{
    static class SocialModeTests
    {
        static World New(Content content, bool companion, bool mischief)
        {
            var w = new World(new SaveData { Mischief = mischief, Scale = 1, Companion = companion, Sound = true }, content, 42);
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

        static double MinPairGap(World w, float seconds)
        {
            double min = double.MaxValue;
            for (int i = 0; i < seconds * 60; i++)
            {
                w.Update(1f / 60);
                min = Math.Min(min, V.Distance(w.Goose.Pos, w.Friend.Pos));
                if (w.Scenes.Active == null)
                    break;
            }

            return min;
        }

        public static void Run(Content content, Action<bool, string> check)
        {
            var fresh = new SaveData();
            check(fresh.Mode == "Normal" && fresh.PreviousMode == "Normal", "Fresh profiles default to Normal with a Normal fallback");
            fresh.Normalize();
            check(fresh.Mode == "Normal" && !fresh.Passive, "Normalized defaults are not Passive");

            var w = New(content, true, false);
            check(w.Data.Mode == "Normal", "Worlds start in Normal mode");
            w.SetMode("Passive");
            check(w.Data.Mode == "Passive" && w.Data.PreviousMode == "Normal", "Entering Passive remembers Normal");
            w.SetQuiet(false);
            check(w.Data.Mode == "Normal", "Leaving Passive restores Normal");
            w.SetMode("Chaos");
            check(w.Data.Mode == "Chaos" && w.Data.PreviousMode == "Chaos", "Chaos is selectable and becomes its own fallback");
            w.SetQuiet(true);
            check(w.Data.Mode == "Passive" && w.Data.PreviousMode == "Chaos", "Entering Passive from Chaos remembers Chaos");
            w.SetQuiet(false);
            check(w.Data.Mode == "Chaos" && !w.Data.Passive && !w.Data.Quiet, "Leaving Passive restores Chaos without enabling anything silently");
            w.SetMode("Loud");
            check(w.Data.Mode == "Chaos", "Unknown modes are ignored");

            w = New(content, false, false);
            w.SetMode("Passive");
            check(!w.StartScene(SceneKind.Trouble, w.Goose), "Trouble cannot start in Passive or solo play");

            w = New(content, true, false);
            w.SetMode("Passive");
            int honks = 0;
            w.Honked += actor => honks++;
            w.Honk();
            check(honks == 0, "Passive suppresses honks");
            w.SetMode("Normal");
            w.Honk();
            check(honks == 1, "Normal sounds honks again");

            w = New(content, true, false);
            w.Goose.Direction = w.Goose.HeadDirection = 0;
            w.Chase(w.Goose, w.Beak(w.Goose));
            w.Update(.01f);
            check(w.Goose.Task == Activity.Chase && !w.HoldingCursor, "Provoked chase runs without capture in Normal");
            w.SetMode("Passive");
            check(w.Goose.Task == Activity.Chase && !w.HoldingCursor, "Provoked chase continues without capture in Passive");
            w.Chase(w.Friend, w.Friend.Pos + new V(100, 0));
            Advance(w, 10);
            check(!w.HoldingCursor && w.Coordinator.CursorOwner == null, "No provoked response captures the cursor in any mode");

            w = New(content, true, false);
            w.SetMode("Passive");
            w.ThrowBall(w.Goose, w.Goose.Pos + new V(90, 0));
            check(w.Goose.Task == Activity.Ball, "Explicit ball play starts in Passive");
            Advance(w, 6);
            check(w.Kicks > 0, "Explicit ball play contacts the ball in Passive");
            w.Fetch(w.Friend, w.Friend.Pos + new V(200, 0));
            Advance(w, 10);
            check(w.Fetches == 1, "Explicit fetch completes in Passive");
            w.Deliver(w.Goose, false);
            Advance(w, 20);
            check(w.Deliveries == 1 && w.Coordinator.DeliveryOwner == null, "Explicit delivery completes in Passive with the pipeline released");
            w.Feed(w.Goose.Pos + new V(60, 0));
            check(w.Goose.Task == Activity.Eat, "Explicit feeding works in Passive");

            w = New(content, true, true);
            w.Data.RandomAttacks = true;
            w.PlaceObjectRaw(ToyKind.Ball, new V(600, 360));
            w.PlaceObjectRaw(ToyKind.Stick, new V(700, 360));
            w.PlaceObjectRaw(ToyKind.Rope, new V(800, 360));
            w.PlaceObject(ToyKind.Pond, new V(450, 360));
            w.Goose.Place(new V(430, 360));
            w.Friend.Place(new V(470, 360));
            Advance(w, 2);
            check(w.Goose.Swimming && w.Friend.Swimming, "Both geese swim together in a shared pond");
            w.SetMode("Passive");
            w.Data.Mischief = true;
            bool everHeld = false;
            int completedBefore = w.Scenes.CompletedScenes;
            int kicksBefore = w.Kicks, fetchesBefore = w.Fetches;
            for (int i = 0; i < 150 * 60; i++)
            {
                w.Update(1f / 60);
                everHeld = everHeld || w.HoldingCursor || w.Coordinator.CursorOwner != null || w.Coordinator.WindowOwner != null;
            }

            check(w.Deliveries == 0, "Relaxation: automatic deliveries stop in Passive");
            check(!everHeld, "Relaxation: no cursor or window interference in Passive");
            check(w.Scenes.CompletedScenes > completedBefore || w.Scenes.Active != null, "Relaxation: peaceful social scenes continue in Passive");
            check(w.Kicks > kicksBefore || w.Fetches > fetchesBefore, "Relaxation: peaceful play continues in Passive");

            w = New(content, true, true);
            w.Data.RandomAttacks = true;
            everHeld = false;
            for (int i = 0; i < 120 * 60; i++)
            {
                w.Update(1f / 60);
                everHeld = everHeld || w.HoldingCursor || w.Coordinator.CursorOwner != null || w.Coordinator.WindowOwner != null;
            }

            check(!everHeld, "Normal autonomy never captures the cursor or touches windows");
            check(w.Deliveries > 0, "Normal autonomy still delivers within its window cap");
            check(w.Scenes.CompletedScenes > 0, "Normal autonomy runs shared scenes");

            w = New(content, true, false);
            check(ForceAccept(w, SceneKind.BallPass, w.Goose), "Ball passing starts through invitation and acceptance");
            var passing = w.Scenes.Active;
            check(passing != null && (passing.Leader == w.Goose || passing.Follower == w.Goose) && (passing.Leader == w.Friend || passing.Follower == w.Friend), "Ball passing involves exactly the two geese");
            check(passing.Invite.State == InvitationState.Accepted, "Accepted invitations are recorded");
            int turns = passing.Turn;
            for (int i = 0; i < 40 * 60 && w.Scenes.Active != null; i++)
            {
                w.Update(1f / 60);
                if (w.Scenes.Active != null)
                    turns = Math.Max(turns, w.Scenes.Active.Turn);
            }

            check(turns >= 2, "Ball passing switches roles between the geese");
            check(w.Kicks > 0 && w.Scenes.Active == null && w.Scenes.LastCompleted == SceneKind.BallPass, "Ball passing ends and returns both actors to ordinary activity");
            check(w.Goose.Task == Activity.Wander && w.Friend.Task == Activity.Wander, "Ball passers settle after the ending");
            check(w.Goose.SocialCooldown > 0 && w.Friend.SocialCooldown > 0, "Endings leave breathing room before the next scene");
            check(w.Coordinator.ReservationCount == 0, "Ball passing releases its reservation");

            w = New(content, true, false);
            check(ForceAccept(w, SceneKind.FetchRelay, w.Friend), "Alternating fetch starts with Canada leading");
            int fetchTurn = w.Scenes.Active.Turn;
            for (int i = 0; i < 60 * 60 && w.Scenes.Active != null; i++)
            {
                w.Update(1f / 60);
                if (w.Scenes.Active != null)
                    fetchTurn = Math.Max(fetchTurn, w.Scenes.Active.Turn);
            }

            check(fetchTurn >= 1, "Fetch alternates turns between the geese");
            check(w.Scenes.Active == null && w.Fetches >= 2, "Alternating fetch completes repeated retrieves and ends");

            w = New(content, true, false);
            check(ForceAccept(w, SceneKind.Tug, w.Goose), "Tug rope play starts through invitation");
            check(w.Scenes.Active.Prop != null && w.Scenes.Active.Prop.Kind == ToyKind.Rope, "Tug reserves the rope");
            RunToEnd(w, 30);
            check(w.Scenes.Active == null && w.Scenes.LastCompleted == SceneKind.Tug, "Tug ends with a release instead of hanging");
            check(w.Objects.Any(o => o.Kind == ToyKind.Rope) && w.Goose.Held == null && w.Friend.Held == null, "The rope survives tug play with a single holder each carry");
            check(w.Coordinator.ReservationCount == 0, "Tug releases its reservation");

            w = New(content, true, false);
            w.PlaceObject(ToyKind.Pond, new V(600, 360));
            w.Goose.Place(new V(560, 360));
            w.Friend.Place(new V(640, 360));
            check(ForceAccept(w, SceneKind.PondCompany, w.Friend), "Pond company starts through invitation");
            double gap = MinPairGap(w, 25);
            check(w.Goose.Swimming && w.Friend.Swimming, "Pond company keeps both geese swimming");
            check(gap > 12, "Pond company keeps loose spacing without overlapping bodies");
            RunToEnd(w, 30);
            check(w.Scenes.Active == null, "Pond company ends with actors returning to ordinary activity");

            w = New(content, true, false);
            w.Goose.Place(new V(560, 360));
            w.Friend.Place(new V(660, 360));
            check(ForceAccept(w, SceneKind.Rest, w.Goose), "Resting nearby starts through invitation");
            double restGap = MinPairGap(w, 25);
            check(restGap > 12, "Resting geese settle near each other without overlapping");
            RunToEnd(w, 30);
            check(w.Scenes.Active == null && w.Goose.Task == Activity.Wander && w.Friend.Task == Activity.Wander, "Rest ends calmly with breathing room");

            w = New(content, true, false);
            check(ForceAccept(w, SceneKind.Pursuit, w.Goose), "Playful pursuit starts through invitation");
            int swaps = 0, lastTurn = w.Scenes.Active.Turn;
            for (int i = 0; i < 30 * 60 && w.Scenes.Active != null; i++)
            {
                w.Update(1f / 60);
                if (w.Scenes.Active != null && w.Scenes.Active.Turn != lastTurn)
                {
                    swaps++;
                    lastTurn = w.Scenes.Active.Turn;
                }
            }

            check(swaps >= 1, "Pursuit swaps the chaser and runner");
            check(w.Scenes.Active == null, "Pursuit ends calmly instead of tailgating forever");

            w = New(content, true, false);
            check(ForceAccept(w, SceneKind.Trouble, w.Goose), "Coordinated trouble starts in Normal");
            Advance(w, 1);
            check(w.Goose.Task == Activity.Social && w.Friend.Task == Activity.Social, "Coordinated trouble involves both geese");
            w.SetMode("Passive");
            check(w.Scenes.Active == null && w.Coordinator.ReservationCount == 0, "Mode switching to Passive strands no trouble reservation");
            check(w.Goose.Task != Activity.Social && w.Friend.Task != Activity.Social, "Mode switching returns actors to ordinary activity");

            w = New(content, true, false);
            check(ForceAccept(w, SceneKind.Trouble, w.Friend), "Trouble restarts for the distraction check");
            Advance(w, 1);
            w.Feed(w.Goose.Pos + new V(80, 0));
            check(w.Scenes.Active == null && w.Goose.Task == Activity.Eat, "A better distraction abandons coordinated trouble");
            check(w.Friend.Task == Activity.Eat || w.Friend.Task == Activity.Wander, "The partner reacts to the distraction instead of sticking");

            w = New(content, true, false);
            check(w.StartScene(SceneKind.Rest, w.Goose), "A rest scene offers its invitation");
            for (int i = 0; i < 200 && w.Scenes.Active != null && w.Scenes.Active.Phase != ScenePhase.Invite; i++)
                w.Update(1f / 60);
            check(w.Scenes.Active != null && w.Scenes.Active.Phase == ScenePhase.Invite, "The invitation phase waits for an answer");
            w.Coordinator.Respond(w.Scenes.Active.Invite.Id, false);
            RunToEnd(w, 10);
            check(w.Scenes.Active == null && w.Goose.Task == Activity.Wander, "Declined invitations end intentionally with no stuck actor");
            check(w.Goose.SocialCooldown > 0, "Declined invitations still breathe before the next scene");

            w = New(content, true, false);
            check(ForceAccept(w, SceneKind.BallPass, w.Goose), "Ball scene restarts for the interruption check");
            Advance(w, 2);
            w.RemoveObject(w.Scenes.Active.Prop);
            check(w.Scenes.Active == null && w.Coordinator.ReservationCount == 0, "Removing a reserved toy ends the scene with no stale reservation");
            check(w.Goose.Task == Activity.Wander && w.Friend.Task == Activity.Wander, "Toy removal returns actors to ordinary activity");

            w = New(content, true, false);
            check(ForceAccept(w, SceneKind.FetchRelay, w.Goose), "Fetch scene restarts for the drag check");
            Advance(w, 2);
            var prop = w.Scenes.Active.Prop;
            w.OnObjectDragged(prop);
            check(w.Scenes.Active == null && prop.Holder == null && !prop.Carried, "Dragging a reserved toy cancels the scene and drops carry state");

            w = New(content, true, false);
            check(ForceAccept(w, SceneKind.Rest, w.Goose), "Rest scene restarts for the pause check");
            Advance(w, 1);
            w.SetPaused(true);
            check(w.Scenes.Active == null && w.Coordinator.ReservationCount == 0, "Pausing strands no reservation");
            w.SetPaused(false);
            Advance(w, 3);
            check(w.Goose.Task == Activity.Wander && w.Friend.Task == Activity.Wander, "Actors resume cleanly after a paused scene");

            w = New(content, true, false);
            check(ForceAccept(w, SceneKind.Rest, w.Goose), "Rest scene restarts for the companion check");
            Advance(w, 1);
            w.SetCompanion(false);
            check(w.Scenes.Active == null && w.Coordinator.ReservationCount == 0, "Disabling Canada releases scene reservations");
            check(w.Coordinator.DeliveryOwner == null && w.Coordinator.CursorOwner == null, "Disabling Canada releases exclusive owners");
            check(w.Goose.Task == Activity.Wander, "Disabling Canada mid-scene leaves white undisturbed");

            w = New(content, true, false);
            check(ForceAccept(w, SceneKind.Rest, w.Friend), "Scene restarts for the clear check");
            Advance(w, 1);
            w.ClearProps();
            check(w.Scenes.Active == null && w.Coordinator.ReservationCount == 0 && w.Objects.Count == 0, "Clearing the desktop ends scenes cleanly");

            w = New(content, true, false);
            for (int i = 0; i < 16; i++)
                w.PlaceObjectRaw(ToyKind.Ball, new V(120 + (i % 4) * 220, 150 + (i / 4) * 130));
            check(w.Objects.Count == 16 && !w.StartScene(SceneKind.Tug, w.Goose), "A full desktop blocks new rope scenes");
            w.RemoveObject(w.Objects[0]);
            check(w.StartScene(SceneKind.Tug, w.Goose), "The rope shares the freed 16-object cap");
            w.Scenes.Cancel(w);
            using (var bmp = new Bitmap(1280, 752))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                var ropeWorld = New(content, false, false);
                var rope = ropeWorld.PlaceObject(ToyKind.Rope, new V(600, 360));
                Art.Interactions(g, ropeWorld);
                bool painted = false;
                for (int dx = -30; dx <= 30 && !painted; dx++)
                    for (int dy = -6; dy <= 6 && !painted; dy++)
                        painted = bmp.GetPixel(600 + dx, 360 + dy).A > 0;
                check(rope != null && painted, "The tug rope renders through the shared object path");
            }

            string root = Store.Root;
            Store.Root = Path.Combine(Program.ArtifactsFolder, "test-modes-" + Guid.NewGuid().ToString("N"));
            try
            {
                check(Store.Save(new SaveData { Sound = false, Companion = true, Mode = "Chaos", PreviousMode = "Chaos" }), "Mode preferences save");
                var loaded = Store.Load();
                check(loaded.Mode == "Chaos" && loaded.PreviousMode == "Chaos" && loaded.Companion, "Mode preferences persist across restarts");
                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Hat\":\"Scarf\",\"Scale\":1.5,\"Quiet\":true}");
                var upgraded = Store.Load();
                check(upgraded.Mode == "Normal" && upgraded.PreviousMode == "Normal" && upgraded.Hat == "Scarf", "Upgraded profiles land on Normal with startup choices preserved");
                check(Store.Save(new SaveData { Mode = "Passive", PreviousMode = "Normal" }), "Passive preferences save");
                check(Store.Save(new SaveData { Mode = "Normal", PreviousMode = "Normal" }), "Normal preferences save");
                File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Mode\":\"Loud\",\"PreviousMode\":\"Normal\"}");
                var recovered = Store.Load();
                check(recovered.Mode == "Passive" && recovered.PreviousMode == "Normal", "Invalid modes recover the previous backup");
            }
            finally
            {
                Store.Root = root;
            }

            string liveRoot = Store.Root;
            Store.Root = Path.Combine(Program.ArtifactsFolder, "test-live-modes-" + Guid.NewGuid().ToString("N"));
            try
            {
                UserContent.Initialize(AppDomain.CurrentDomain.BaseDirectory, null, false);
                var live = New(new Content(Store.Root), true, false);
                using (var monitor = new ContentMonitor(live))
                {
                    File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Mode\":\"Passive\",\"PreviousMode\":\"Chaos\",\"Scale\":1.5}");
                    monitor.Reload();
                    check(live.Data.Mode == "Passive" && live.Data.PreviousMode == "Chaos" && live.Data.Quiet, "Live settings reload applies modes");
                    File.WriteAllText(Path.Combine(Store.Root, "settings.json"), "{\"Version\":2,\"Mode\":\"Loud\"}");
                    monitor.Reload();
                    check(live.Data.Mode == "Passive" && live.Content.Warnings.Count > 0, "Invalid live modes retain the last valid configuration");
                }
            }
            finally
            {
                Store.Root = liveRoot;
            }
        }
    }
}
