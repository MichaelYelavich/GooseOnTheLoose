using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace SecondHonk
{
    public enum Activity
    {
        Wander,
        Chase,
        Eat,
        Ball,
        Fetch,
        Return,
        Mud,
        Deliver,
        Sleep,
        Follow
    }

    public sealed class Goose
    {
        public V Pos, Velocity, Target, LeftFoot, RightFoot, FootOrigin, FootDestination;
        public float Direction, HeadDirection, Neck, FootTime, StepDuration = .2f, Wait, TaskTime;
        public int MovingFoot = -1;
        public bool FeetReady, Swimming;
        public Coating Coating;
        public int TrailRemaining;
        public Activity Task = Activity.Wander;
        public Goose(V p)
        {
            Pos = Target = p;
            Direction = 0;
        }

        public void Place(V p)
        {
            Pos = Target = p;
            Velocity = new V();
            FeetReady = false;
            MovingFoot = -1;
        }
    }

    public enum Coating
    {
        Dry,
        Wet,
        Mud
    }

    public enum ToyKind
    {
        Ball,
        Stick,
        Peas,
        Duck,
        Pond,
        Puddle,
        Mud
    }

    public sealed class DesktopObject
    {
        public Guid Id = Guid.NewGuid();
        public ToyKind Kind;
        public V Pos, Size, Velocity;
        public bool Carried, Dragging;
        public float Age, Cooldown;
        public bool Surface
        {
            get
            {
                return Kind == ToyKind.Pond || Kind == ToyKind.Puddle || Kind == ToyKind.Mud;
            }
        }

        public bool Contains(V p)
        {
            V d = p - Pos;
            return Surface ? (d.X * d.X / (Size.X * Size.X * .25f) + d.Y * d.Y / (Size.Y * Size.Y * .25f) <= 1) : d.Length < Math.Max(Size.X, Size.Y) * .5f;
        }
    }

    public sealed class Mark
    {
        public V Pos;
        public float Age, Angle, Scale = 1;
        public Coating Coating = Coating.Mud;
    }
}
