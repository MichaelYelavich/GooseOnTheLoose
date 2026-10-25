using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace SecondHonk
{
    public sealed class World
    {
        public readonly SaveData Data;
        public readonly Content Content;
        public readonly Goose Goose = new Goose(new V(350, 300), 0, "White"), Friend = new Goose(new V(270, 330), 1, "Canada");
        public readonly List<Mark> Marks = new List<Mark>();
        public readonly List<DesktopObject> Objects = new List<DesktopObject>();
        public readonly PlayCoordinator Coordinator = new PlayCoordinator();
        public Behavior Behavior = new Behavior();
        public bool Editing, Exploding;
        public float DismissRemaining, ExplosionTime;
        DesktopObject ball, stick;
        readonly Random random;
        int noteIndex, imageIndex;
        public float Width = 1280, Height = 752, Time, GrabRemaining, QuietRemaining;
        public V Pointer, GrabTarget;
        public bool Paused;
        public int Meals, Fetches, Kicks, Deliveries;
        public string DeliveryText = "", DeliveryImage = "";
        public event Action<Goose> Honked, FootLanded, CursorCaught, DeliveryStarted, DeliveryCompleted;
        public float Scale
        {
            get
            {
                return Data.Scale;
            }
        }

        public float GooseScale(Goose goose)
        {
            return Scale * (goose == Friend ? .85f : 1);
        }

        public Goose GrabOwner
        {
            get
            {
                return Coordinator.CursorOwner;
            }
        }

        public int DeliveryStage
        {
            get
            {
                Goose owner = Coordinator.DeliveryOwner;
                return (owner ?? Goose).DeliveryStage;
            }

            set
            {
                Goose owner = Coordinator.DeliveryOwner;
                (owner ?? Goose).DeliveryStage = value;
            }
        }

        public int DeliverySide
        {
            get
            {
                Goose owner = Coordinator.DeliveryOwner;
                return (owner ?? Goose).DeliverySide;
            }

            set
            {
                Goose owner = Coordinator.DeliveryOwner;
                (owner ?? Goose).DeliverySide = value;
            }
        }

        public V BallPos
        {
            get
            {
                return ball == null ? new V() : ball.Pos;
            }

            set
            {
                if (ball != null)
                    ball.Pos = value;
            }
        }

        public V BallVelocity
        {
            get
            {
                return ball == null ? new V() : ball.Velocity;
            }

            set
            {
                if (ball != null)
                    ball.Velocity = value;
            }
        }

        public V StickPos
        {
            get
            {
                return stick == null ? new V() : stick.Pos;
            }

            set
            {
                if (stick != null)
                    stick.Pos = value;
            }
        }

        public bool BallVisible
        {
            get
            {
                return ball != null && Objects.Contains(ball);
            }

            set
            {
                if (!value && ball != null)
                {
                    Objects.Remove(ball);
                    ball = null;
                }
            }
        }

        public bool StickVisible
        {
            get
            {
                return stick != null && Objects.Contains(stick);
            }

            set
            {
                if (!value && stick != null)
                {
                    Objects.Remove(stick);
                    stick = null;
                }
            }
        }

        public bool CarryingStick
        {
            get
            {
                return stick != null && stick.Carried;
            }

            set
            {
                if (stick != null && !value)
                    Coordinator.ReleaseCarry(Goose.Held == stick ? Goose : Friend.Held == stick ? Friend : null, stick);
                else if (stick != null && value)
                    Coordinator.TryAcquireCarry(Goose, stick);
            }
        }

        public bool HoldingCursor
        {
            get
            {
                return GrabRemaining > 0 && !Paused && !Data.Quiet && Data.MouseSteal;
            }
        }

        public World(SaveData data, Content content, int seed)
        {
            Data = data;
            Content = content;
            random = new Random(seed);
            Goose.Boldness = .7f;
            Goose.Sociability = .45f;
            Friend.Boldness = .45f;
            Friend.Sociability = .65f;
        }

        public void ApplyBehavior(Behavior behavior)
        {
            Behavior = behavior;
            Goose.Deck.Clear();
            Friend.Deck.Clear();
            Goose.AutoIn = Behavior.PrankMinSeconds;
            Friend.AutoIn = Behavior.PrankMinSeconds;
        }

        public V Clamp(V p)
        {
            return new V(Math.Max(35 * Scale, Math.Min(Width - 35 * Scale, p.X)), Math.Max(65 * Scale, Math.Min(Height - 20 * Scale, p.Y)));
        }

        public V ConstrainObject(DesktopObject o, V p)
        {
            if (!o.Surface)
                return Clamp(p);
            o.Size = new V(Math.Min(o.Size.X, Width - 32), Math.Min(o.Size.Y, Height - 32));
            float x = o.Size.X / 2 + 14, y = o.Size.Y / 2 + 14;
            return new V(Math.Max(x, Math.Min(Width - x, p.X)), Math.Max(y, Math.Min(Height - y, p.Y)));
        }

        public V Beak(Goose g)
        {
            float extend = g.Neck;
            V forward = V.Direction(g.HeadDirection);
            return g.Pos + new V(0, -14 * Scale) + V.Direction(g.Direction) * (15 * Scale) + forward * ((11 + 13 * extend) * Scale) + new V(0, -(20 - 10 * extend) * Scale);
        }

        public void Resize(float width, float height)
        {
            Width = Math.Max(400, width);
            Height = Math.Max(300, height);
            Goose.Place(Clamp(Goose.Pos));
            Friend.Place(Clamp(Friend.Pos));
            ClearProps();
            Cancel(Goose);
        }

        public void ClearProps()
        {
            CancelAll();
            Coordinator.Clear();
            Objects.Clear();
            ball = stick = null;
            Marks.Clear();
            Goose.Coating = Friend.Coating = Coating.Dry;
            Goose.TrailRemaining = Friend.TrailRemaining = 0;
            Goose.Swimming = Friend.Swimming = false;
        }

        public void Cancel()
        {
            CancelActor(Goose);
        }

        public void Cancel(Goose actor)
        {
            CancelActor(actor);
        }

        public void CancelAll()
        {
            CancelActor(Goose);
            CancelActor(Friend);
            ReleaseCursorAll();
        }

        public void CancelActor(Goose actor)
        {
            if (actor == null)
                return;
            if (actor.Task == Activity.Deliver)
                Coordinator.ReleaseDelivery(actor);
            SetTaskActor(actor, Activity.Wander);
            if (Coordinator.CursorOwner == actor)
            {
                GrabRemaining = 0;
                Coordinator.ReleaseCursor(actor);
            }
        }

        public void CancelDelivery()
        {
            Goose owner = Coordinator.DeliveryOwner;
            if (owner == null)
                owner = Goose.Task == Activity.Deliver ? Goose : Friend.Task == Activity.Deliver ? Friend : null;
            if (owner != null)
                CancelActor(owner);
        }

        public void ReleaseCursorAll()
        {
            GrabRemaining = 0;
            Coordinator.CursorOwner = null;
        }

        public void ReleaseCursorFor(Goose actor)
        {
            if (actor == null)
                return;
            if (Coordinator.CursorOwner == null || Coordinator.CursorOwner == actor)
            {
                GrabRemaining = 0;
                Coordinator.ReleaseCursor(actor);
            }
        }

        public void SetTask(Activity task)
        {
            SetTaskActor(Goose, task);
        }

        public void SetTask(Goose actor, Activity task)
        {
            SetTaskActor(actor, task);
        }

        void SetTaskActor(Goose actor, Activity task)
        {
            if (actor == null)
                return;
            if (task != Activity.Fetch && task != Activity.Return && actor.Held != null)
                Coordinator.ReleaseCarry(actor, actor.Held);
            if (actor.TargetObject != null && actor.TargetObject != actor.Held)
            {
                Coordinator.Release(actor.TargetObject);
                actor.TargetObject = null;
            }

            actor.Task = task;
            actor.TaskTime = 0;
            actor.Wait = 0;
            if (task == Activity.Deliver)
                actor.DeliveryStage = 0;
        }

        public DesktopObject PlaceObject(ToyKind kind, V p)
        {
            var o = PlaceObjectRaw(kind, p);
            if (o == null)
                return null;
            if (kind == ToyKind.Ball)
            {
                ball = o;
                SetTaskActor(Goose, Activity.Ball);
                if (Coordinator.TryReserve(o, Goose))
                    Goose.TargetObject = o;
            }

            if (kind == ToyKind.Stick)
            {
                Coordinator.ReleaseCarry(Goose, Goose.Held);
                Coordinator.ReleaseCarry(Friend, Friend.Held);
                stick = o;
                Goose.ReturnPoint = Goose.Pos;
                SetTaskActor(Goose, Activity.Fetch);
                if (Coordinator.TryReserve(o, Goose))
                    Goose.TargetObject = o;
            }

            if (kind == ToyKind.Peas)
            {
                ReleaseCursorFor(Goose);
                SetTaskActor(Goose, Activity.Eat);
            }

            return o;
        }

        public DesktopObject PlaceObjectRaw(ToyKind kind, V p)
        {
            if (Objects.Count >= 16)
                return null;
            var o = new DesktopObject
            {
                Kind = kind,
                Pos = Clamp(p),
                Size = kind == ToyKind.Pond ? new V(230, 145) : kind == ToyKind.Puddle || kind == ToyKind.Mud ? new V(135, 80) : kind == ToyKind.Stick ? new V(48, 24) : kind == ToyKind.Peas ? new V(32, 28) : kind == ToyKind.Duck ? new V(48, 36) : new V(36, 36)
            };
            o.Pos = ConstrainObject(o, p);
            Objects.Add(o);
            return o;
        }

        public void RemoveObject(DesktopObject o)
        {
            if (o == null)
                return;
            Objects.Remove(o);
            Coordinator.Release(o);
            if (o == ball)
                ball = null;
            if (o == stick)
                stick = null;
            foreach (var actor in new[] { Goose, Friend })
            {
                if (actor.TargetObject == o)
                    actor.TargetObject = null;
                if (actor.Held == o)
                    actor.Held = null;
                if (actor.Task == Activity.Ball || actor.Task == Activity.Fetch || actor.Task == Activity.Return || actor.Task == Activity.Eat)
                    CancelActor(actor);
            }
        }

        public void OnObjectDragged(DesktopObject o)
        {
            if (o == null)
                return;
            ReleaseCursorAll();
            Coordinator.Release(o);
            if (o == ball)
                ball = null;
            if (o == stick)
                stick = null;
            foreach (var actor in new[] { Goose, Friend })
            {
                if (actor.TargetObject == o)
                    actor.TargetObject = null;
                if (actor.Held == o)
                    actor.Held = null;
                if (actor.Task == Activity.Ball || actor.Task == Activity.Fetch || actor.Task == Activity.Return || actor.Task == Activity.Eat)
                    CancelActor(actor);
            }
        }

        public void SetCompanion(bool enabled)
        {
            Data.Companion = enabled;
            if (!enabled)
            {
                Coordinator.ReleaseAllFor(Friend);
                SetTaskActor(Friend, Activity.Wander);
                Friend.Target = Friend.Pos;
            }
            else
            {
                Friend.Place(Clamp(Goose.Pos + new V(-70, 25)));
                SetTaskActor(Friend, Activity.Wander);
            }
        }

        public void SetPaused(bool paused)
        {
            if (Paused == paused)
            {
                if (paused)
                    ReleaseCursorAll();
                return;
            }

            Paused = paused;
            ReleaseCursorAll();
            if (paused)
            {
                Coordinator.ReleaseUnpossessed();
                foreach (var actor in new[] { Goose, Friend })
                    if (actor.TargetObject != null && Coordinator.OwnerOf(actor.TargetObject) != actor)
                        actor.TargetObject = null;
            }
        }

        public void Honk()
        {
            Honk(Goose);
        }

        public void Honk(Goose actor)
        {
            if (actor == null)
                return;
            if (!Data.Quiet && Data.Sound && Honked != null)
                Honked(actor);
        }

        public void Chase(V p)
        {
            Chase(Goose, p);
        }

        public void Chase(Goose actor, V p)
        {
            if (actor == null || Paused || Data.Quiet)
                return;
            Pointer = Clamp(p);
            ReleaseCursorFor(actor);
            SetTaskActor(actor, Activity.Chase);
            Honk(actor);
        }

        public void Follow(V p)
        {
            Follow(Goose, p);
        }

        public void Follow(Goose actor, V p)
        {
            if (actor == null || Paused)
                return;
            Pointer = Clamp(p);
            ReleaseCursorFor(actor);
            SetTaskActor(actor, Activity.Follow);
        }

        public void Call(V p)
        {
            Call(Goose, p);
        }

        public void Call(Goose actor, V p)
        {
            if (actor == null)
                return;
            ReleaseCursorFor(actor);
            SetTaskActor(actor, Activity.Follow);
            Pointer = Clamp(p);
            Honk(actor);
        }

        public void Feed(V p)
        {
            if (Paused)
                return;
            PlaceObject(ToyKind.Peas, p);
        }

        public void Feed(Goose actor, V p)
        {
            if (actor == null || Paused)
                return;
            var o = PlaceObjectRaw(ToyKind.Peas, p);
            if (o == null)
                return;
            ReleaseCursorFor(actor);
            SetTaskActor(actor, Activity.Eat);
        }

        public void ThrowBall(V p)
        {
            ThrowBall(Goose, p);
        }

        public void ThrowBall(Goose actor, V p)
        {
            if (actor == null || Paused || Data.Quiet)
                return;
            ReleaseCursorFor(actor);
            DesktopObject o;
            if (actor == Goose)
            {
                o = PlaceObject(ToyKind.Ball, p);
            }
            else
            {
                o = PlaceObjectRaw(ToyKind.Ball, p);
                if (o != null)
                {
                    SetTaskActor(actor, Activity.Ball);
                    if (Coordinator.TryReserve(o, actor))
                        actor.TargetObject = o;
                }
            }

            if (o != null)
                o.Velocity = new V(random.Next(-80, 81), random.Next(-80, 81));
            actor.BallCooldown = 0;
            Honk(actor);
        }

        public void Fetch(V p)
        {
            Fetch(Goose, p);
        }

        public void Fetch(Goose actor, V p)
        {
            if (actor == null || Paused || Data.Quiet)
                return;
            ReleaseCursorFor(actor);
            if (actor == Goose)
            {
                PlaceObject(ToyKind.Stick, p);
            }
            else
            {
                var o = PlaceObjectRaw(ToyKind.Stick, p);
                if (o != null)
                {
                    actor.ReturnPoint = actor.Pos;
                    SetTaskActor(actor, Activity.Fetch);
                    if (Coordinator.TryReserve(o, actor))
                        actor.TargetObject = o;
                }
            }

            Honk(actor);
        }

        public void MakeMud()
        {
            MakeMud(Goose);
        }

        public void MakeMud(Goose actor)
        {
            if (actor == null || Paused || Data.Quiet)
                return;
            ReleaseCursorFor(actor);
            actor.Coating = Coating.Mud;
            actor.TrailRemaining = Behavior.TrailSteps;
            SetTaskActor(actor, Activity.Mud);
            actor.Target = RandomPoint();
            Honk(actor);
        }

        public void Deliver(bool image)
        {
            Deliver(Goose, image);
        }

        public void Deliver(Goose actor, bool image)
        {
            if (actor == null || Paused || Data.Quiet)
                return;
            if (Coordinator.DeliveryOwner != null && Coordinator.DeliveryOwner != actor)
                return;
            if (!Coordinator.TryAcquireDelivery(actor))
                return;
            ReleaseCursorFor(actor);
            DeliveryText = Content.Notes.Count == 0 ? "Have a honk." : Content.Notes[(noteIndex++ & int.MaxValue) % Content.Notes.Count];
            DeliveryImage = image && Content.Images.Count > 0 ? Content.Images[(imageIndex++ & int.MaxValue) % Content.Images.Count] : "";
            actor.DeliverySide = actor.Pos.X < Width / 2 ? -1 : 1;
            SetTaskActor(actor, Activity.Deliver);
            actor.Target = new V(actor.DeliverySide < 0 ? -75 * Scale : Width + 75 * Scale, Math.Max(120 * Scale, Math.Min(Height - 100 * Scale, actor.Pos.Y)));
            Honk(actor);
        }

        public void SetQuiet(bool quiet)
        {
            Data.Quiet = quiet;
            QuietRemaining = 0;
            ReleaseCursorAll();
            CancelAll();
        }

        V RandomPoint()
        {
            return Clamp(new V((float)random.NextDouble() * Width, (float)random.NextDouble() * Height));
        }

        void ChooseActivity(Goose actor)
        {
            if (actor.Deck.Count == 0)
            {
                var choices = new List<int>();
                int[] weights =
                {
                    Behavior.MudWeight,
                    Behavior.NoteWeight,
                    Behavior.PictureWeight,
                    Behavior.ChaseWeight,
                    Behavior.ToyWeight
                };
                for (int k = 0; k < weights.Length; k++)
                    for (int j = 0; j < weights[k]; j++)
                        choices.Add(k);
                if (choices.Count == 0)
                    return;
                while (choices.Count > 0)
                {
                    int i = random.Next(choices.Count);
                    actor.Deck.Enqueue(choices[i]);
                    choices.RemoveAt(i);
                }
            }

            switch (actor.Deck.Dequeue())
            {
                case 0:
                    MakeMud(actor);
                    break;
                case 1:
                    Deliver(actor, false);
                    break;
                case 2:
                    Deliver(actor, true);
                    break;
                case 3:
                    if (Data.RandomAttacks)
                        Chase(actor, Pointer);
                    break;
                case 4:
                    DesktopObject found = null;
                    if (actor.TargetObject != null && actor.TargetObject.Kind == ToyKind.Ball && Objects.Contains(actor.TargetObject) && !actor.TargetObject.Dragging && !Coordinator.IsReservedByOther(actor.TargetObject, actor))
                        found = actor.TargetObject;
                    else
                        found = Objects.Where(o => o.Kind == ToyKind.Ball && !o.Dragging && !Coordinator.IsReservedByOther(o, actor)).OrderBy(o => V.Distance(actor.Pos, o.Pos)).FirstOrDefault();
                    if (found != null)
                    {
                        if (actor == Goose)
                            ball = found;
                        Coordinator.TryReserve(found, actor);
                        actor.TargetObject = found;
                        SetTaskActor(actor, Activity.Ball);
                    }
                    else if (actor == Goose && Data.Companion)
                    {
                        SetTaskActor(actor, Activity.Follow);
                        Pointer = Friend.Pos;
                    }

                    break;
            }
        }

        void Move(Goose g, V target, float speed, float dt, bool offscreen)
        {
            UpdateSurface(g);
            if (g.Swimming)
                speed *= .65f;
            if (!offscreen)
                target = Clamp(target);
            V delta = target - g.Pos;
            V desired = delta.Length > 4 * Scale ? delta.Unit * Math.Min(speed * Scale, delta.Length * 5) : new V();
            V change = desired - g.Velocity;
            float acceleration = (g.Task == Activity.Chase ? 2300 : 1300) * Scale * dt;
            if (change.Length > acceleration)
                change = change.Unit * acceleration;
            g.Velocity += change;
            g.Pos += g.Velocity * dt;
            if (!offscreen)
                g.Pos = Clamp(g.Pos);
            UpdateSurface(g);
            if (g.Velocity.Length > 6)
                g.Direction = V.Turn(g.Direction, V.Angle(g.Velocity), dt * 3);
            float headTarget = g.Direction;
            if (V.Distance(Pointer, g.Pos) < 140 * Scale && !HoldingCursor && g.Task != Activity.Deliver)
                headTarget = V.Turn(g.Direction, V.Angle(Pointer - g.Pos), .65f);
            g.Attention = V.Distance(Pointer, g.Pos) < 140 * Scale && g.Task != Activity.Deliver ? Pointer : target;
            g.HeadDirection = V.Turn(g.HeadDirection, headTarget, dt * 5);
            float neckTarget = g.Task == Activity.Chase || g.Task == Activity.Fetch || g.Task == Activity.Return ? 1 : 0;
            g.Neck += (neckTarget - g.Neck) * Math.Min(1, dt * 6);
            SolveFeet(g, dt);
        }

        void SolveFeet(Goose g, float dt)
        {
            float scale = GooseScale(g);
            V side = V.Direction(g.Direction + (float)Math.PI / 2), leftHome = g.Pos - side * (3 * scale), rightHome = g.Pos + side * (3 * scale);
            if (!g.FeetReady)
            {
                g.LeftFoot = leftHome;
                g.RightFoot = rightHome;
                g.FeetReady = true;
                g.MovingFoot = -1;
            }

            if (g.Swimming)
            {
                g.LeftFoot = leftHome;
                g.RightFoot = rightHome;
                g.MovingFoot = -1;
                return;
            }

            if (g.MovingFoot < 0)
            {
                float ld = V.Distance(g.LeftFoot, leftHome), rd = V.Distance(g.RightFoot, rightHome);
                if (Math.Max(ld, rd) > 5 * scale)
                {
                    g.MovingFoot = ld >= rd ? 0 : 1;
                    g.FootOrigin = g.MovingFoot == 0 ? g.LeftFoot : g.RightFoot;
                    g.FootDestination = (g.MovingFoot == 0 ? leftHome : rightHome) + g.Velocity * .055f;
                    g.FootTime = 0;
                    g.StepDuration = g.Velocity.Length > 150 * Scale ? .1f : .2f;
                }
            }
            else
            {
                g.FootTime += dt;
                float t = Math.Min(1, g.FootTime / g.StepDuration), ease = t < .5f ? 4 * t * t * t : 1 - (float)Math.Pow(-2 * t + 2, 3) / 2;
                V foot = g.FootOrigin + (g.FootDestination - g.FootOrigin) * ease;
                if (g.MovingFoot == 0)
                    g.LeftFoot = foot;
                else
                    g.RightFoot = foot;
                if (t >= 1)
                {
                    LandFoot(g, foot);
                    if (!g.Swimming && Data.Sound && !Data.Quiet && FootLanded != null)
                        FootLanded(g);
                    g.MovingFoot = -1;
                }
            }
        }

        public void UpdateSurface(Goose g)
        {
            var surface = Objects.LastOrDefault(o => o.Surface && o.Contains(g.Pos));
            bool swimming = surface != null && surface.Kind == ToyKind.Pond;
            if (swimming != g.Swimming)
            {
                g.FeetReady = false;
                g.MovingFoot = -1;
            }

            g.Swimming = swimming;
            if (surface != null)
            {
                g.Coating = surface.Kind == ToyKind.Mud ? Coating.Mud : Coating.Wet;
                g.TrailRemaining = Behavior.TrailSteps;
            }
        }

        public void LandFoot(Goose g, V foot)
        {
            if (g.Swimming || Objects.Any(o => o.Surface && o.Contains(foot)))
                return;
            if (g.Coating != Coating.Dry && g.TrailRemaining > 0)
            {
                Marks.Add(new Mark { Pos = new V((float)Math.Round(foot.X), (float)Math.Round(foot.Y)), Angle = g.Direction, Scale = GooseScale(g), Coating = g.Coating });
                g.TrailRemaining--;
                if (g.TrailRemaining == 0)
                    g.Coating = Coating.Dry;
                while (Marks.Count > 256)
                    Marks.RemoveAt(0);
            }
        }

        public void AdvanceQuiet(float elapsed)
        {
            if (QuietRemaining > 0)
            {
                QuietRemaining -= elapsed;
                if (QuietRemaining <= 0)
                    SetQuiet(false);
            }
        }

        public void Update(float dt)
        {
            dt = Math.Max(0, Math.Min(.1f, dt));
            AdvanceQuiet(dt);
            if (!Data.MouseSteal)
            {
                GrabRemaining = 0;
                Coordinator.CursorOwner = null;
            }

            if (Paused)
            {
                GrabRemaining = 0;
                Coordinator.CursorOwner = null;
                if (!Goose.FeetReady)
                    SolveFeet(Goose, 0);
                if (Data.Companion && !Friend.FeetReady)
                    SolveFeet(Friend, 0);
                return;
            }

            Time += dt;
            Goose.TaskTime += dt;
            if (Data.Companion)
                Friend.TaskTime += dt;
            Goose.BallCooldown -= dt;
            if (Data.Companion)
                Friend.BallCooldown -= dt;
            Coordinator.Update(dt);
            for (int i = Marks.Count - 1; i >= 0; i--)
            {
                Marks[i].Age += dt;
                if (Marks[i].Age >= Behavior.FootprintSeconds)
                    Marks.RemoveAt(i);
            }

            if (Marks.Count > 256)
                Marks.RemoveRange(0, Marks.Count - 256);
            foreach (var o in Objects)
            {
                o.Age += dt;
                o.Cooldown -= dt;
                if (o.Carried || o.Dragging || o.Surface)
                    continue;
                o.Pos += o.Velocity * dt;
                o.Velocity *= (float)Math.Exp(-1.1f * dt);
                V constrained = Clamp(o.Pos);
                if (constrained.X != o.Pos.X)
                    o.Velocity.X *= -.75f;
                if (constrained.Y != o.Pos.Y)
                    o.Velocity.Y *= -.75f;
                o.Pos = constrained;
                if (o.Kind == ToyKind.Duck && Objects.Any(s => s.Kind == ToyKind.Pond && s.Contains(o.Pos)))
                    o.Pos += new V((float)Math.Sin(Time + o.Pos.Y) * dt * 4, 0);
                if (o.Kind == ToyKind.Ball || o.Kind == ToyKind.Duck)
                    foreach (var g in Data.Companion ? new[]
                    {
                        Goose,
                        Friend
                    }

                    : new[]
                    {
                        Goose
                    }

                    )
                        if (o.Cooldown <= 0 && V.Distance(g.Pos, o.Pos) < 35 * Scale)
                        {
                            o.Velocity = (o.Pos - g.Pos).Unit * Behavior.NudgeSpeed * Scale;
                            o.Cooldown = 1;
                            Kicks++;
                        }
            }

            if (Data.Quiet)
                foreach (var actor in Data.Companion ? new[] { Goose, Friend } : new[] { Goose })
                    if (actor.Task == Activity.Chase || actor.Task == Activity.Mud || actor.Task == Activity.Deliver || actor.Task == Activity.Fetch || actor.Task == Activity.Ball)
                        CancelActor(actor);
            StepActor(Goose, dt);
            if (Data.Companion)
                StepActor(Friend, dt);
        }

        void StepActor(Goose actor, float dt)
        {
            V target = actor.Pos;
            float speed = 80;
            bool offscreen = false;
            switch (actor.Task)
            {
                case Activity.Wander:
                    if (V.Distance(actor.Pos, actor.Target) < 10 * Scale)
                    {
                        actor.Wait -= dt;
                        if (actor.Wait <= 0)
                        {
                            actor.Target = RandomPoint();
                            actor.Wait = 1 + (float)random.NextDouble() * 2;
                        }
                    }

                    target = actor.Target;
                    actor.AutoIn -= dt;
                    actor.NapIn -= dt;
                    if (Objects.Any(o => o.Kind == ToyKind.Peas))
                        SetTaskActor(actor, Activity.Eat);
                    else if (actor.AutoIn <= 0)
                    {
                        actor.AutoIn = Behavior.PrankMinSeconds + (float)random.NextDouble() * (Behavior.PrankMaxSeconds - Behavior.PrankMinSeconds);
                        if (Data.Mischief && !Data.Quiet)
                            ChooseActivity(actor);
                    }

                    if (actor.NapIn <= 0)
                    {
                        actor.NapIn = 180 + random.Next(180);
                        SetTaskActor(actor, Activity.Sleep);
                        actor.Target = new V(Width - 70 * Scale, Height - 30 * Scale);
                    }

                    if (actor == Friend && actor.Task == Activity.Wander)
                    {
                        V follow = Goose.Pos - V.Direction(Goose.Direction) * (65 * Scale) + new V(0, 15 * Scale);
                        float comfort = 40 + 60 * actor.Sociability;
                        if (V.Distance(actor.Pos, follow) > comfort * Scale)
                        {
                            target = follow;
                            speed = 140;
                        }
                    }

                    break;
                case Activity.Chase:
                    speed = 400;
                    if (HoldingCursor && Coordinator.CursorOwner == actor)
                    {
                        GrabRemaining -= dt;
                        target = GrabTarget;
                        if (GrabRemaining <= 0)
                            CancelActor(actor);
                        else if (V.Distance(actor.Pos, target) < 20 * Scale)
                            CancelActor(actor);
                    }
                    else
                    {
                        target = Clamp(Pointer - (Beak(actor) - actor.Pos));
                        if (Data.MouseSteal && actor.TaskTime < 9 && V.Distance(Beak(actor), Pointer) < 18 * Scale && (Coordinator.CursorOwner == null || Coordinator.CursorOwner == actor))
                        {
                            Coordinator.TryAcquireCursor(actor);
                            GrabRemaining = 1.4f;
                            GrabTarget = Clamp(actor.Pos + V.Direction((float)random.NextDouble() * 6.28f) * (340 * Scale));
                            if (Data.Sound && CursorCaught != null)
                                CursorCaught(actor);
                        }

                        if (actor.TaskTime > 9)
                            CancelActor(actor);
                    }

                    break;
                case Activity.Follow:
                    target = Pointer;
                    speed = 160;
                    if (actor.TaskTime > 8)
                        CancelActor(actor);
                    break;
                case Activity.Eat:
                    DesktopObject pea = null;
                    if (actor.TargetObject != null && actor.TargetObject.Kind == ToyKind.Peas && Objects.Contains(actor.TargetObject) && !actor.TargetObject.Dragging && !Coordinator.IsReservedByOther(actor.TargetObject, actor))
                        pea = actor.TargetObject;
                    else
                        pea = Objects.Where(o => o.Kind == ToyKind.Peas && !o.Dragging && !Coordinator.IsReservedByOther(o, actor)).OrderBy(p => V.Distance(p.Pos, actor.Pos)).FirstOrDefault();
                    if (pea == null)
                    {
                        CancelActor(actor);
                        break;
                    }

                    Coordinator.TryReserve(pea, actor);
                    actor.TargetObject = pea;
                    target = pea.Pos;
                    speed = Behavior.ToyApproachSpeed;
                    if (V.Distance(Beak(actor), target) < 35 * Scale || V.Distance(actor.Pos, target) < 12 * Scale)
                    {
                        Objects.Remove(pea);
                        Coordinator.Release(pea);
                        actor.TargetObject = null;
                        Meals++;
                    }

                    break;
                case Activity.Ball:
                    DesktopObject toy = null;
                    if (actor.TargetObject != null && actor.TargetObject.Kind == ToyKind.Ball && Objects.Contains(actor.TargetObject) && !actor.TargetObject.Dragging && !Coordinator.IsReservedByOther(actor.TargetObject, actor))
                        toy = actor.TargetObject;
                    else
                        toy = Objects.Where(o => o.Kind == ToyKind.Ball && !o.Dragging && !Coordinator.IsReservedByOther(o, actor)).OrderBy(o => V.Distance(actor.Pos, o.Pos)).FirstOrDefault();
                    if (toy == null || actor.TaskTime > 18)
                    {
                        ball = Objects.FirstOrDefault(o => o.Kind == ToyKind.Ball && !o.Dragging);
                        CancelActor(actor);
                        break;
                    }

                    Coordinator.TryReserve(toy, actor);
                    actor.TargetObject = toy;
                    ball = toy;
                    if (toy.Dragging)
                        break;
                    target = Clamp(toy.Pos - (Beak(actor) - actor.Pos));
                    speed = Behavior.ToyApproachSpeed;
                    if ((V.Distance(Beak(actor), toy.Pos) < 28 * Scale || V.Distance(actor.Pos, toy.Pos) < 24 * Scale) && actor.BallCooldown <= 0)
                    {
                        toy.Velocity = (toy.Pos - actor.Pos).Unit * (260 * Scale);
                        actor.BallCooldown = 1;
                        Kicks++;
                        Honk(actor);
                    }

                    break;
                case Activity.Fetch:
                    DesktopObject fetch = null;
                    if (actor.TargetObject != null && actor.TargetObject.Kind == ToyKind.Stick && Objects.Contains(actor.TargetObject) && !actor.TargetObject.Dragging && !Coordinator.IsReservedByOther(actor.TargetObject, actor))
                        fetch = actor.TargetObject;
                    else
                        fetch = Objects.Where(o => o.Kind == ToyKind.Stick && !o.Dragging && !Coordinator.IsReservedByOther(o, actor)).OrderBy(o => V.Distance(actor.Pos, o.Pos)).FirstOrDefault();
                    if (fetch == null)
                    {
                        CancelActor(actor);
                        break;
                    }

                    Coordinator.TryReserve(fetch, actor);
                    actor.TargetObject = fetch;
                    stick = fetch;
                    target = fetch.Pos;
                    speed = Behavior.ToyApproachSpeed;
                    if (!fetch.Dragging && V.Distance(actor.Pos, target) < 25 * Scale)
                    {
                        if (Coordinator.TryAcquireCarry(actor, fetch))
                            SetTaskActor(actor, Activity.Return);
                        else
                            CancelActor(actor);
                    }

                    break;
                case Activity.Return:
                    target = actor.ReturnPoint;
                    speed = 180;
                    DesktopObject held = actor.Held;
                    if (held == null)
                    {
                        CancelActor(actor);
                        break;
                    }

                    held.Pos = Beak(actor);
                    stick = held;
                    if (V.Distance(actor.Pos, target) < 18 * Scale)
                    {
                        Fetches++;
                        Coordinator.ReleaseCarry(actor, held);
                        held.Pos = Clamp(Beak(actor));
                        CancelActor(actor);
                        Honk(actor);
                    }

                    break;
                case Activity.Mud:
                    target = actor.Target;
                    speed = 200;
                    if (V.Distance(actor.Pos, target) < 30 * Scale)
                        actor.Target = RandomPoint();
                    if (actor.TaskTime > 12)
                        CancelActor(actor);
                    break;
                case Activity.Deliver:
                    offscreen = true;
                    target = actor.Target;
                    if (actor.DeliveryStage == 0 && V.Distance(actor.Pos, target) < 20 * Scale)
                    {
                        actor.DeliveryStage = 1;
                        actor.WaitOffscreen = .8f;
                    }
                    else if (actor.DeliveryStage == 1)
                    {
                        actor.WaitOffscreen -= dt;
                        if (actor.WaitOffscreen <= 0)
                        {
                            actor.DeliveryStage = 2;
                            actor.Target = Clamp(new V(Width * (actor.DeliverySide < 0 ? .48f : .52f), Height * .55f));
                            if (DeliveryStarted != null)
                                DeliveryStarted(actor);
                        }
                    }
                    else if (actor.DeliveryStage == 2 && V.Distance(actor.Pos, target) < 15 * Scale)
                    {
                        Deliveries++;
                        Coordinator.ReleaseDelivery(actor);
                        CancelActor(actor);
                        if (DeliveryCompleted != null)
                            DeliveryCompleted(actor);
                    }

                    break;
                case Activity.Sleep:
                    target = actor.Target;
                    speed = 55;
                    if (actor.TaskTime > 25)
                        CancelActor(actor);
                    break;
            }

            Move(actor, target, speed, dt, offscreen);
        }
    }
}
