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
        public readonly int Id;
        public readonly string Name;
        public V Pos, Velocity, Target, Attention, LeftFoot, RightFoot, FootOrigin, FootDestination;
        public float Direction, HeadDirection, Neck, FootTime, StepDuration = .2f, Wait, TaskTime;
        public float AutoIn = 20, NapIn = 180, BallCooldown, WaitOffscreen;
        public readonly Queue<int> Deck = new Queue<int>();
        public int DeliveryStage, DeliverySide;
        public V ReturnPoint;
        public DesktopObject Held, TargetObject;
        public float Boldness = .5f, Sociability = .5f;
        public int MovingFoot = -1;
        public bool FeetReady, Swimming;
        public Coating Coating;
        public int TrailRemaining;
        public Activity Task = Activity.Wander;
        public Goose(V p, int id, string name)
        {
            Pos = Target = Attention = p;
            ReturnPoint = p;
            Id = id;
            Name = name;
            Direction = 0;
        }

        public Goose(V p) : this(p, 0, "White")
        {
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
        public Goose Holder;
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
