using System;
using System.Collections.Generic;

namespace SecondHonk
{
    // How a manipulation must treat the target. The broker always demands
    // all three: size, activation (focus) and Z-order are preserved, so a
    // prank never resizes, focuses or restacks another application.
    [Flags]
    public enum WindowMoveFlags
    {
        None = 0,
        PreserveSize = 1,
        PreserveActivation = 2,
        PreserveOrder = 4
    }

    // A manipulated window behind the broker. Headless tests use fakes; the
    // shipped game wraps real top-level windows (see SystemWindows.cs).
    public interface IWindowTarget
    {
        string Id
        {
            get;
        }
        bool IsValid
        {
            get;
        }
        bool IsEligible
        {
            get;
        }
        bool IsForeground
        {
            get;
        }
        bool UserHeld
        {
            get;
        }
        V Position
        {
            get;
        }
        V Size
        {
            get;
        }
        // Applies the move with the given preservation flags. Returns false
        // when the system rejected the movement (closed, reused or
        // inaccessible target), which cancels the manipulation.
        bool MoveTo(V position, WindowMoveFlags flags);
    }

    // Source of candidate windows for the simulation's prank requests.
    public interface IWindowSystem
    {
        IWindowTarget ActiveWindow();
        IEnumerable<IWindowTarget> InactiveWindows();
        V DisplaySize
        {
            get;
        }
    }

    // The single broker for all real-window manipulation, owned alongside
    // the PlayCoordinator reservations. The simulation requests pranks; the
    // broker validates the CURRENT mode, the target and the timing, then
    // applies movement in bounded steps:
    //
    // - Normal: small nudges to the ACTIVE window only. Nothing else.
    // - Chaos: drags of INACTIVE windows to a new reachable position.
    // - Passive: no window manipulation at all.
    //
    // One manipulation at a time, with cooldowns between manipulations. A
    // single pending request may wait while one runs; anything else is
    // refused. When the user grabs, moves or resizes a window, any
    // in-progress or queued manipulation of that window cancels immediately,
    // and that window earns a grace period before it is eligible again.
    // Positions are never restored afterward: the window stays where the
    // goose left it.
    public sealed class WindowBroker
    {
        // Card 4 tuning: poke distance, drag length, step durations and the
        // pauses between manipulations.
        public const float NudgeMaxDistance = 10;
        public const float NudgeDuration = .35f;
        public const float NudgeCooldown = 45;
        public const float DragMaxDistance = 140;
        public const float DragDuration = 1.1f;
        public const float DragCooldown = 75;
        public const float UserGraceSeconds = 30;
        public const WindowMoveFlags RequiredFlags = WindowMoveFlags.PreserveSize | WindowMoveFlags.PreserveActivation | WindowMoveFlags.PreserveOrder;

        public enum OperationKind
        {
            Nudge,
            Drag
        }

        public sealed class Operation
        {
            public IWindowTarget Target;
            public OperationKind Kind;
            public V From, To;
            public float Duration, Elapsed;
        }

        public sealed class PendingRequest
        {
            public IWindowTarget Target;
            public OperationKind Kind;
            public V Offset, Destination;
            public Func<string> Mode;
        }

        public Operation Active;
        public PendingRequest Pending;
        public float CooldownRemaining;
        public float Time;
        public V Display = new V(1280, 752);
        readonly Dictionary<string, float> grace = new Dictionary<string, float>();

        public bool Busy
        {
            get
            {
                return Active != null || Pending != null;
            }
        }

        public bool TargetInGrace(string id)
        {
            float until;
            return id != null && grace.TryGetValue(id, out until) && until > Time;
        }

        public bool TryNudge(IWindowTarget target, V offset, Func<string> mode)
        {
            return Request(target, OperationKind.Nudge, offset, new V(), mode);
        }

        public bool TryDrag(IWindowTarget target, V destination, Func<string> mode)
        {
            return Request(target, OperationKind.Drag, new V(), destination, mode);
        }

        bool Request(IWindowTarget target, OperationKind kind, V offset, V destination, Func<string> mode)
        {
            if (target == null || mode == null || !target.IsValid || !target.IsEligible || TargetInGrace(target.Id))
                return false;
            string current = mode();
            if (kind == OperationKind.Nudge)
            {
                if (current != "Normal" && current != "Chaos")
                    return false;
                if (!target.IsForeground)
                    return false;
            }
            else
            {
                if (current != "Chaos")
                    return false;
                if (target.IsForeground)
                    return false;
            }

            var request = new PendingRequest { Target = target, Kind = kind, Offset = offset, Destination = destination, Mode = mode };
            if (!ValidateStart(request, current))
                return false;
            if (Active != null)
            {
                if (Pending != null)
                    return false;
                Pending = request;
                return true;
            }

            // Cooldowns separate manipulations: nothing queues across one.
            if (CooldownRemaining > 0)
                return false;
            Start(request, current);
            return true;
        }

        bool ValidateStart(PendingRequest request, string mode)
        {
            if (request.Kind == OperationKind.Nudge)
            {
                if (mode != "Normal" && mode != "Chaos")
                    return false;
                if (!request.Target.IsForeground)
                    return false;
                return true;
            }

            if (mode != "Chaos")
                return false;
            if (request.Target.IsForeground)
                return false;
            V clamped = ClampOnDisplay(request.Destination, request.Target.Size);
            return clamped.X != request.Target.Position.X || clamped.Y != request.Target.Position.Y;
        }

        void Start(PendingRequest request, string mode)
        {
            V from = request.Target.Position;
            V to = request.Kind == OperationKind.Nudge ? ClampNudge(from, request.Offset, request.Target.Size) : ClampOnDisplay(request.Destination, request.Target.Size);
            Active = new Operation
            {
                Target = request.Target,
                Kind = request.Kind,
                From = from,
                To = to,
                Duration = request.Kind == OperationKind.Nudge ? NudgeDuration : DragDuration,
                Elapsed = 0
            };
        }

        V ClampNudge(V from, V offset, V size)
        {
            V step = offset;
            if (step.Length > NudgeMaxDistance)
                step = step.Unit * NudgeMaxDistance;
            return ClampOnDisplay(from + step, size);
        }

        V ClampOnDisplay(V p, V size)
        {
            float w = Display.X - size.X, h = Display.Y - size.Y;
            if (w < 0)
                w = 0;
            if (h < 0)
                h = 0;
            return new V(Math.Max(0, Math.Min(w, p.X)), Math.Max(0, Math.Min(h, p.Y)));
        }

        public void Update(float dt)
        {
            dt = Math.Max(0, Math.Min(.1f, dt));
            Time += dt;
            if (CooldownRemaining > 0)
                CooldownRemaining = Math.Max(0, CooldownRemaining - dt);
            var expired = new List<string>();
            foreach (var pair in grace)
                if (pair.Value <= Time)
                    expired.Add(pair.Key);
            foreach (string id in expired)
                grace.Remove(id);
            if (Active == null)
            {
                if (Pending != null && CooldownRemaining <= 0)
                {
                    var next = Pending;
                    Pending = null;
                    if (next.Target.IsValid && next.Target.IsEligible && !next.Target.UserHeld && !TargetInGrace(next.Target.Id) && ValidateStart(next, next.Mode()))
                        Start(next, next.Mode());
                }

                return;
            }

            var op = Active;
            if (!op.Target.IsValid || !op.Target.IsEligible || op.Target.UserHeld)
            {
                CancelActive(op, true);
                return;
            }

            // Recheck foreground status during the prank: a drag target that
            // became active must no longer receive an inactive-sized move,
            // and a nudge target that lost activation is left alone.
            if (op.Kind == OperationKind.Drag ? op.Target.IsForeground : !op.Target.IsForeground)
            {
                CancelActive(op, true);
                return;
            }

            // An externally moved target (same handle, new geometry) ends the
            // prank instead of chasing it.
            if (V.Distance(op.Target.Position, op.From + (op.To - op.From) * Math.Min(1, op.Elapsed / op.Duration)) > NudgeMaxDistance + 4 && op.Elapsed > 0)
            {
                CancelActive(op, true);
                return;
            }

            op.Elapsed += dt;
            float t = Math.Min(1, op.Elapsed / op.Duration);
            V at = op.From + (op.To - op.From) * t;
            if (!op.Target.MoveTo(at, RequiredFlags))
            {
                CancelActive(op, false);
                return;
            }

            if (t >= 1)
            {
                Active = null;
                CooldownRemaining = op.Kind == OperationKind.Nudge ? NudgeCooldown : DragCooldown;
            }
        }

        void CancelActive(Operation op, bool touchedByUser)
        {
            Active = null;
            CooldownRemaining = op.Kind == OperationKind.Nudge ? NudgeCooldown : DragCooldown;
            if (touchedByUser)
                grace[op.Target.Id] = Time + UserGraceSeconds;
        }

        // The user grabbed, moved or resized a window: any in-progress or
        // queued manipulation of that window cancels immediately, and the
        // window earns a grace period. Other windows are unaffected.
        public void NotifyUserIntervention(string id)
        {
            if (String.IsNullOrEmpty(id))
                return;
            grace[id] = Time + UserGraceSeconds;
            if (Active != null && Active.Target.Id == id)
            {
                CooldownRemaining = Active.Kind == OperationKind.Nudge ? NudgeCooldown : DragCooldown;
                Active = null;
            }

            if (Pending != null && Pending.Target.Id == id)
                Pending = null;
        }

        public void NotifyUserIntervention(IWindowTarget target)
        {
            if (target != null)
                NotifyUserIntervention(target.Id);
        }

        public void CancelAll()
        {
            Active = null;
            Pending = null;
        }
    }
}
