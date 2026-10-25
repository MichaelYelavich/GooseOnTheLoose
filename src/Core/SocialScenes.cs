using System;
using System.Collections.Generic;
using System.Linq;

namespace SecondHonk
{
    public enum SceneKind
    {
        BallPass,
        FetchRelay,
        Tug,
        PondCompany,
        Rest,
        Pursuit,
        Trouble
    }

    public enum ScenePhase
    {
        Notice,
        Invite,
        Activity,
        Ending
    }

    public sealed class SocialScene
    {
        public SceneKind Kind;
        public ScenePhase Phase;
        public Goose Leader, Follower;
        public Invitation Invite;
        public DesktopObject Prop;
        public float Timer, ExchangeIn, AnswerIn, EndIn;
        public int Turn, Exchanges, ExchangesNeeded;
        public V SpotA, SpotB, SpotMid;
        public bool Intrusive, SecondHonkDone, Carrying;
    }

    // Pair-play scenes with an invitation, a response, an activity and a
    // recognizable ending. Peaceful scenes run in every mode; Trouble (joint
    // cursor chasing) is user-directed intrusion and never starts in Passive.
    // No scene captures the cursor or touches other applications' windows:
    // those mechanics belong to Card 4. Simulation-side only, seeded.
    public sealed class SocialScenes
    {
        public SocialScene Active;
        public int CompletedScenes;
        public SceneKind LastCompleted;
        readonly Random random;

        public SocialScenes(int seed)
        {
            random = new Random(seed ^ 0x5EED);
        }

        public bool IsParticipant(Goose g)
        {
            return Active != null && g != null && (g == Active.Leader || g == Active.Follower);
        }

        public bool Start(World w, SceneKind kind)
        {
            return Start(w, kind, null);
        }

        public bool Start(World w, SceneKind kind, Goose leader)
        {
            if (Active != null || w == null || !w.Data.Companion || w.Paused)
                return false;
            if (kind == SceneKind.Trouble && w.Data.Passive)
                return false;
            Goose first = leader ?? (random.NextDouble() < w.Goose.Boldness / (w.Goose.Boldness + w.Friend.Boldness) ? w.Goose : w.Friend);
            Goose second = first == w.Goose ? w.Friend : w.Goose;
            if (first.Task != Activity.Wander || second.Task != Activity.Wander || first.Held != null || second.Held != null)
                return false;
            if (first.SocialCooldown > 0 || second.SocialCooldown > 0)
                return false;
            if (!PrepareProp(w, kind, first))
                return false;
            var scene = new SocialScene
            {
                Kind = kind,
                Phase = ScenePhase.Notice,
                Leader = first,
                Follower = second,
                Timer = .5f + (float)random.NextDouble() * .4f,
                Intrusive = kind == SceneKind.Trouble
            };
            Active = scene;
            w.SetTask(first, Activity.Social);
            first.Attention = second.Pos;
            return true;
        }

        bool PrepareProp(World w, SceneKind kind, Goose seeker)
        {
            switch (kind)
            {
                case SceneKind.BallPass:
                    return FindOrPlace(w, ToyKind.Ball) != null;
                case SceneKind.FetchRelay:
                    return FindOrPlace(w, ToyKind.Stick) != null;
                case SceneKind.Tug:
                    return FindOrPlace(w, ToyKind.Rope) != null;
                case SceneKind.PondCompany:
                    return w.Objects.Any(o => o.Kind == ToyKind.Pond);
                default:
                    return true;
            }
        }

        DesktopObject FindOrPlace(World w, ToyKind kind)
        {
            var found = w.Objects.Where(o => o.Kind == kind && !o.Dragging && !o.Carried).OrderBy(o => V.Distance(w.Goose.Pos, o.Pos)).FirstOrDefault();
            if (found != null)
                return found;
            if (w.Objects.Count >= 16)
                return null;
            return w.PlaceObjectRaw(kind, (w.Goose.Pos + w.Friend.Pos) / 2);
        }

        public void Cancel(World w)
        {
            var scene = Active;
            if (scene == null)
                return;
            Active = null;
            ReleaseScene(w, scene);
            foreach (var actor in new[] { scene.Leader, scene.Follower })
            {
                if (actor == null)
                    continue;
                if (actor.Task == Activity.Social)
                    w.SetTask(actor, Activity.Wander);
                actor.SocialCooldown = Math.Max(actor.SocialCooldown, 10);
            }
        }

        public void CancelIntrusive(World w)
        {
            if (Active != null && Active.Intrusive)
                Cancel(w);
        }

        void ReleaseScene(World w, SocialScene scene)
        {
            if (scene.Prop != null)
            {
                if (scene.Prop.Holder != null)
                    w.Coordinator.ReleaseCarry(scene.Prop.Holder, scene.Prop);
                w.Coordinator.Release(scene.Prop);
                scene.Prop = null;
            }

            w.Coordinator.CancelInvitationsFor(scene.Leader);
            w.Coordinator.CancelInvitationsFor(scene.Follower);
        }

        void Finish(World w, SocialScene scene)
        {
            Active = null;
            ReleaseScene(w, scene);
            CompletedScenes++;
            LastCompleted = scene.Kind;
            foreach (var actor in new[] { scene.Leader, scene.Follower })
            {
                if (actor == null)
                    continue;
                if (actor.Task == Activity.Social)
                    w.SetTask(actor, Activity.Wander);
                actor.SocialCooldown = 25 + (float)random.NextDouble() * 25;
            }
        }

        public void Update(World w, float dt)
        {
            w.Goose.SocialCooldown = Math.Max(0, w.Goose.SocialCooldown - dt);
            if (w.Data.Companion)
                w.Friend.SocialCooldown = Math.Max(0, w.Friend.SocialCooldown - dt);
            if (Active == null)
            {
                MaybeTrigger(w, dt);
                return;
            }

            var scene = Active;
            if (!w.Data.Companion || scene.Leader.Task != Activity.Social)
            {
                Cancel(w);
                return;
            }

            if ((scene.Phase == ScenePhase.Activity || scene.Phase == ScenePhase.Ending) && scene.Follower.Task != Activity.Social)
            {
                Cancel(w);
                return;
            }

            if (scene.Prop != null && (!w.Objects.Contains(scene.Prop) || scene.Prop.Dragging))
            {
                Cancel(w);
                return;
            }

            if (scene.Kind == SceneKind.Trouble && w.Data.Passive)
            {
                Cancel(w);
                return;
            }

            if (scene.Kind == SceneKind.Trouble && w.Objects.Any(o => o.Kind == ToyKind.Peas))
            {
                Cancel(w);
                w.SetTask(scene.Leader, Activity.Eat);
                w.SetTask(scene.Follower, Activity.Eat);
                return;
            }

            switch (scene.Phase)
            {
                case ScenePhase.Notice:
                    scene.Leader.Attention = scene.Follower.Pos;
                    scene.Timer -= dt;
                    if (scene.Timer <= 0)
                    {
                        scene.Invite = w.Coordinator.Offer(scene.Leader, scene.Follower, scene.Kind.ToString(), 6);
                        if (scene.Invite == null)
                        {
                            Cancel(w);
                            return;
                        }

                        scene.Phase = ScenePhase.Invite;
                        scene.AnswerIn = .8f + (float)random.NextDouble() * 1.2f;
                    }

                    break;
                case ScenePhase.Invite:
                    scene.Leader.Attention = scene.Follower.Pos;
                    if (scene.Invite.State == InvitationState.Accepted)
                    {
                        BeginActivity(w, scene);
                        break;
                    }

                    if (scene.Invite.State == InvitationState.Declined || scene.Invite.State == InvitationState.Expired || scene.Invite.State == InvitationState.Cancelled)
                    {
                        scene.Phase = ScenePhase.Ending;
                        scene.EndIn = 1.2f;
                        break;
                    }

                    scene.AnswerIn -= dt;
                    if (scene.AnswerIn <= 0 && scene.Invite.State == InvitationState.Offered)
                    {
                        bool free = scene.Follower.Task == Activity.Wander || scene.Follower.Task == Activity.Social;
                        double accept = 0.45 + 0.4 * scene.Follower.Sociability;
                        if (free && random.NextDouble() < accept)
                        {
                            w.Coordinator.Respond(scene.Invite.Id, true);
                            BeginActivity(w, scene);
                        }
                        else
                        {
                            w.Coordinator.Respond(scene.Invite.Id, false);
                            scene.Phase = ScenePhase.Ending;
                            scene.EndIn = 1.2f;
                        }
                    }

                    break;
                case ScenePhase.Activity:
                    if (!StepActivity(w, scene, dt))
                        return;
                    break;
                case ScenePhase.Ending:
                    scene.EndIn -= dt;
                    if (scene.EndIn <= 0)
                        Finish(w, scene);
                    break;
            }
        }

        void MaybeTrigger(World w, float dt)
        {
            if (!w.Data.Companion || !w.Data.Mischief || Active != null)
                return;
            Goose a = w.Goose, b = w.Friend;
            if (a.Task != Activity.Wander || b.Task != Activity.Wander || a.Held != null || b.Held != null)
                return;
            if (a.SocialCooldown > 0 || b.SocialCooldown > 0)
                return;
            if (random.NextDouble() > dt / 22.0)
                return;
            var options = new List<Tuple<SceneKind, int>>();
            options.Add(Tuple.Create(SceneKind.Rest, 2));
            options.Add(Tuple.Create(SceneKind.Pursuit, 2));
            if (w.Objects.Any(o => o.Kind == ToyKind.Pond))
                options.Add(Tuple.Create(SceneKind.PondCompany, 2));
            if (w.Objects.Any(o => o.Kind == ToyKind.Ball && !o.Dragging) || w.Objects.Count < 16)
                options.Add(Tuple.Create(SceneKind.BallPass, 3));
            if (w.Objects.Any(o => o.Kind == ToyKind.Stick && !o.Dragging) || w.Objects.Count < 16)
                options.Add(Tuple.Create(SceneKind.FetchRelay, 3));
            if (w.Objects.Any(o => o.Kind == ToyKind.Rope && !o.Dragging) || w.Objects.Count < 16)
                options.Add(Tuple.Create(SceneKind.Tug, 2));
            if (!w.Data.Passive)
                options.Add(Tuple.Create(SceneKind.Trouble, 1));
            int total = options.Sum(o => o.Item2), roll = random.Next(total);
            foreach (var option in options)
            {
                roll -= option.Item2;
                if (roll < 0)
                {
                    Start(w, option.Item1);
                    return;
                }
            }
        }

        void BeginActivity(World w, SocialScene scene)
        {
            scene.Phase = ScenePhase.Activity;
            scene.Turn = 0;
            scene.Exchanges = 0;
            scene.ExchangeIn = 0;
            scene.Carrying = false;
            scene.SecondHonkDone = false;
            w.SetTask(scene.Follower, Activity.Social);
            V mid = (scene.Leader.Pos + scene.Follower.Pos) / 2;
            switch (scene.Kind)
            {
                case SceneKind.BallPass:
                    scene.Prop = FindOrPlace(w, ToyKind.Ball);
                    scene.ExchangesNeeded = 2 + random.Next(3);
                    scene.Timer = 30;
                    break;
                case SceneKind.FetchRelay:
                    scene.Prop = FindOrPlace(w, ToyKind.Stick);
                    scene.ExchangesNeeded = 2 + random.Next(2);
                    scene.SpotMid = w.Clamp(mid);
                    scene.Timer = 40;
                    break;
                case SceneKind.Tug:
                    scene.Prop = FindOrPlace(w, ToyKind.Rope);
                    scene.Timer = 3.5f + (float)random.NextDouble() * 1.5f;
                    break;
                case SceneKind.PondCompany:
                    scene.Prop = w.Objects.Where(o => o.Kind == ToyKind.Pond).OrderBy(o => V.Distance(mid, o.Pos)).FirstOrDefault();
                    scene.SpotA = w.Clamp(scene.Prop.Pos + new V(-45 * w.Scale, 8 * w.Scale));
                    scene.SpotB = w.Clamp(scene.Prop.Pos + new V(45 * w.Scale, -8 * w.Scale));
                    scene.Timer = 6 + (float)random.NextDouble() * 4;
                    break;
                case SceneKind.Rest:
                    scene.SpotA = w.Clamp(mid + new V(-42 * w.Scale, 8 * w.Scale));
                    scene.SpotB = w.Clamp(mid + new V(42 * w.Scale, -8 * w.Scale));
                    scene.Timer = 8 + (float)random.NextDouble() * 6;
                    break;
                case SceneKind.Pursuit:
                    scene.Timer = 14;
                    scene.ExchangeIn = 2.5f + (float)random.NextDouble() * 1.5f;
                    break;
                case SceneKind.Trouble:
                    scene.Timer = 6 + (float)random.NextDouble() * 3;
                    break;
            }

            if ((scene.Kind == SceneKind.BallPass || scene.Kind == SceneKind.FetchRelay || scene.Kind == SceneKind.Tug) && scene.Prop == null)
            {
                Cancel(w);
                return;
            }

            if (scene.Prop != null)
            {
                w.Coordinator.Release(scene.Prop);
                w.Coordinator.TryReserve(scene.Prop, scene.Leader);
            }

            w.Honk(scene.Leader);
            if (scene.Kind == SceneKind.Trouble)
                w.Honk(scene.Follower);
        }

        // Returns false when the scene ended or was cancelled mid-step.
        bool StepActivity(World w, SocialScene scene, float dt)
        {
            scene.Timer -= dt;
            scene.ExchangeIn = Math.Max(0, scene.ExchangeIn - dt);
            switch (scene.Kind)
            {
                case SceneKind.BallPass:
                case SceneKind.FetchRelay:
                case SceneKind.Tug:
                    if (scene.Prop == null || !w.Objects.Contains(scene.Prop) || scene.Prop.Dragging)
                    {
                        Cancel(w);
                        return false;
                    }

                    break;
            }

            switch (scene.Kind)
            {
                case SceneKind.BallPass:
                    if (scene.Timer <= 0 || scene.Exchanges >= scene.ExchangesNeeded)
                    {
                        scene.Phase = ScenePhase.Ending;
                        scene.EndIn = 1.5f + (float)random.NextDouble();
                        return true;
                    }

                    break;
                case SceneKind.FetchRelay:
                    if (scene.Timer <= 0 || scene.Exchanges >= scene.ExchangesNeeded)
                    {
                        if (scene.Carrying && scene.Prop.Holder != null)
                            w.Coordinator.ReleaseCarry(scene.Prop.Holder, scene.Prop);
                        scene.Carrying = false;
                        scene.Phase = ScenePhase.Ending;
                        scene.EndIn = 1.5f + (float)random.NextDouble();
                        return true;
                    }

                    break;
                case SceneKind.Tug:
                    if (scene.Timer <= 0)
                    {
                        w.Honk(scene.Turn % 2 == 0 ? scene.Leader : scene.Follower);
                        scene.Phase = ScenePhase.Ending;
                        scene.EndIn = 1.5f + (float)random.NextDouble();
                        return true;
                    }

                    break;
                case SceneKind.PondCompany:
                    if (scene.Leader.Swimming && scene.Follower.Swimming && scene.Timer <= 0)
                    {
                        scene.Phase = ScenePhase.Ending;
                        scene.EndIn = 1.5f;
                        return true;
                    }

                    if (scene.Timer < -8)
                    {
                        scene.Phase = ScenePhase.Ending;
                        scene.EndIn = 1.2f;
                        return true;
                    }

                    break;
                case SceneKind.Rest:
                    if (scene.Timer <= 0)
                    {
                        scene.Phase = ScenePhase.Ending;
                        scene.EndIn = 1.5f + (float)random.NextDouble();
                        return true;
                    }

                    break;
                case SceneKind.Pursuit:
                    if (scene.Timer <= 0 || scene.Turn >= 2)
                    {
                        scene.Phase = ScenePhase.Ending;
                        scene.EndIn = 2;
                        return true;
                    }

                    if (scene.ExchangeIn <= 0)
                    {
                        scene.Turn++;
                        scene.ExchangeIn = 2.5f + (float)random.NextDouble() * 1.5f;
                    }

                    break;
                case SceneKind.Trouble:
                    if (!scene.SecondHonkDone && scene.Timer < 3)
                    {
                        w.Honk(scene.Leader);
                        scene.SecondHonkDone = true;
                    }

                    if (scene.Timer <= 0)
                    {
                        scene.Phase = ScenePhase.Ending;
                        scene.EndIn = 1.5f;
                        return true;
                    }

                    break;
            }

            return true;
        }

        public void Steer(World w, Goose actor, float dt, ref V target, ref float speed, ref bool offscreen)
        {
            offscreen = false;
            var scene = Active;
            if (scene == null || (actor != scene.Leader && actor != scene.Follower))
            {
                target = actor.Pos;
                speed = 0;
                return;
            }

            Goose other = actor == scene.Leader ? scene.Follower : scene.Leader;
            target = actor.Pos;
            speed = 0;
            switch (scene.Phase)
            {
                case ScenePhase.Notice:
                case ScenePhase.Invite:
                    if (actor == scene.Leader)
                    {
                        target = actor.Pos;
                        speed = 0;
                    }
                    else
                    {
                        target = actor.Target;
                        speed = 80;
                    }

                    return;
                case ScenePhase.Ending:
                    target = actor.Pos + new V(12 * w.Scale, 0);
                    speed = 40;
                    return;
            }

            switch (scene.Kind)
            {
                case SceneKind.BallPass:
                    SteerBallPass(w, scene, actor, other, ref target, ref speed);
                    break;
                case SceneKind.FetchRelay:
                    SteerFetch(w, scene, actor, other, ref target, ref speed);
                    break;
                case SceneKind.Tug:
                    SteerTug(w, scene, actor, other, ref target, ref speed);
                    break;
                case SceneKind.PondCompany:
                    SteerPond(w, scene, actor, ref target, ref speed);
                    break;
                case SceneKind.Rest:
                    SteerRest(w, scene, actor, ref target, ref speed);
                    break;
                case SceneKind.Pursuit:
                    SteerPursuit(w, scene, actor, other, ref target, ref speed);
                    break;
                case SceneKind.Trouble:
                    target = w.Clamp(w.Pointer - (w.Beak(actor) - actor.Pos));
                    speed = 300;
                    break;
            }

            if (scene.Kind != SceneKind.Tug && other.Task == Activity.Social && V.Distance(actor.Pos, other.Pos) < 34 * w.Scale)
            {
                V away = actor.Pos - other.Pos;
                if (away.Length < 1)
                    away = new V(1, 0);
                target = actor.Pos + away.Unit * (44 * w.Scale);
                speed = Math.Max(speed, 60);
            }
        }

        Goose Passer(SocialScene scene)
        {
            return scene.Turn % 2 == 0 ? scene.Leader : scene.Follower;
        }

        void SteerBallPass(World w, SocialScene scene, Goose actor, Goose other, ref V target, ref float speed)
        {
            if (scene.Prop == null)
                return;
            if (actor == Passer(scene) && scene.ExchangeIn <= 0)
            {
                target = w.Clamp(scene.Prop.Pos - (w.Beak(actor) - actor.Pos));
                speed = w.Behavior.ToyApproachSpeed;
                if (V.Distance(w.Beak(actor), scene.Prop.Pos) < 28 * w.Scale || V.Distance(actor.Pos, scene.Prop.Pos) < 24 * w.Scale)
                {
                    V push = scene.Prop.Pos - actor.Pos;
                    if (push.Length < 1)
                        push = other.Pos - actor.Pos;
                    scene.Prop.Velocity = push.Unit * (260 * w.Scale);
                    scene.Prop.Cooldown = 1;
                    w.Kicks++;
                    w.Honk(actor);
                    scene.Turn++;
                    scene.Exchanges++;
                    scene.ExchangeIn = 1.2f;
                    w.Coordinator.Release(scene.Prop);
                    w.Coordinator.TryReserve(scene.Prop, Passer(scene));
                }
            }
            else
            {
                target = w.Clamp(scene.Prop.Pos + (actor.Pos - scene.Prop.Pos).Unit * (60 * w.Scale));
                if ((actor.Pos - scene.Prop.Pos).Length < 1)
                    target = w.Clamp(scene.Prop.Pos + new V(60 * w.Scale, 0));
                speed = 120;
            }
        }

        void SteerFetch(World w, SocialScene scene, Goose actor, Goose other, ref V target, ref float speed)
        {
            if (scene.Prop == null)
                return;
            Goose fetcher = Passer(scene);
            if (actor != fetcher)
            {
                target = w.Clamp(scene.SpotMid + (actor.Pos - scene.SpotMid).Unit * (50 * w.Scale));
                if ((actor.Pos - scene.SpotMid).Length < 1)
                    target = w.Clamp(scene.SpotMid + new V(50 * w.Scale, 0));
                speed = 100;
                return;
            }

            if (scene.Carrying)
            {
                if (actor.Held != scene.Prop)
                {
                    scene.Carrying = false;
                    return;
                }

                scene.Prop.Pos = w.Beak(actor);
                target = scene.SpotMid;
                speed = 180;
                if (V.Distance(actor.Pos, scene.SpotMid) < 20 * w.Scale)
                {
                    scene.Prop.Pos = w.Clamp(w.Beak(actor));
                    w.Coordinator.ReleaseCarry(actor, scene.Prop);
                    scene.Carrying = false;
                    w.Fetches++;
                    w.Honk(actor);
                    scene.Turn++;
                    scene.Exchanges++;
                    scene.SpotMid = w.Clamp((scene.Leader.Pos + scene.Follower.Pos) / 2);
                    w.Coordinator.Release(scene.Prop);
                    w.Coordinator.TryReserve(scene.Prop, Passer(scene));
                }

                return;
            }

            w.Coordinator.TryReserve(scene.Prop, actor);
            target = scene.Prop.Pos;
            speed = w.Behavior.ToyApproachSpeed;
            if (!scene.Prop.Dragging && V.Distance(actor.Pos, scene.Prop.Pos) < 25 * w.Scale)
            {
                if (w.Coordinator.TryAcquireCarry(actor, scene.Prop))
                    scene.Carrying = true;
            }
        }

        void SteerTug(World w, SocialScene scene, Goose actor, Goose other, ref V target, ref float speed)
        {
            if (scene.Prop == null)
                return;
            V end = scene.Prop.Pos + (actor == scene.Leader ? new V(-30 * w.Scale, 0) : new V(30 * w.Scale, 0));
            bool aReady = V.Distance(w.Beak(scene.Leader), scene.Prop.Pos + new V(-28 * w.Scale, 0)) < 34 * w.Scale;
            bool bReady = V.Distance(w.Beak(scene.Follower), scene.Prop.Pos + new V(28 * w.Scale, 0)) < 34 * w.Scale;
            if (aReady && bReady)
            {
                target = actor.Pos;
                speed = 0;
                actor.Direction = V.Angle(other.Pos - actor.Pos);
                actor.HeadDirection = V.Turn(actor.HeadDirection, actor.Direction, .2f);
                scene.Prop.Pos += new V((float)Math.Sin(w.Time * 6 + (actor == scene.Leader ? 0 : Math.PI)) * 12 * w.Scale * .016f, 0);
            }
            else
            {
                target = w.Clamp(end - (w.Beak(actor) - actor.Pos));
                speed = w.Behavior.ToyApproachSpeed;
            }
        }

        void SteerPond(World w, SocialScene scene, Goose actor, ref V target, ref float speed)
        {
            V spot = actor == scene.Leader ? scene.SpotA : scene.SpotB;
            bool settled = V.Distance(scene.Leader.Pos, scene.SpotA) < 24 * w.Scale && V.Distance(scene.Follower.Pos, scene.SpotB) < 24 * w.Scale;
            if (!settled)
            {
                target = spot;
                speed = 140;
                return;
            }

            if (scene.Timer > 0)
            {
                target = actor.Pos;
                speed = 0;
                return;
            }

            target = w.Clamp(spot + new V(0, -120 * w.Scale));
            speed = 120;
        }

        void SteerRest(World w, SocialScene scene, Goose actor, ref V target, ref float speed)
        {
            V spot = actor == scene.Leader ? scene.SpotA : scene.SpotB;
            if (V.Distance(actor.Pos, spot) > 20 * w.Scale)
            {
                target = spot;
                speed = 55;
                return;
            }

            target = actor.Pos;
            speed = 0;
        }

        void SteerPursuit(World w, SocialScene scene, Goose actor, Goose other, ref V target, ref float speed)
        {
            Goose chaser = scene.Turn % 2 == 0 ? scene.Leader : scene.Follower;
            if (actor == chaser)
            {
                target = other.Pos;
                speed = 265;
            }
            else
            {
                V away = actor.Pos - other.Pos;
                if (away.Length < 1)
                    away = new V(1, 0);
                target = w.Clamp(actor.Pos + away.Unit * (130 * w.Scale));
                speed = 240;
            }
        }
    }
}
