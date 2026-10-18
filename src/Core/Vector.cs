using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace SecondHonk
{
    public struct V
    {
        public float X, Y;
        public V(float x, float y)
        {
            X = x;
            Y = y;
        }
        public float Length
        {
            get
            {
                return (float)Math.Sqrt(X * X + Y * Y);
            }
        }
        public V Unit
        {
            get
            {
                return Length < .001f ? new V(1, 0) : this / Length;
            }
        }
        public static V operator +(V a, V b)
        {
            return new V(a.X + b.X, a.Y + b.Y);
        }
        public static V operator -(V a, V b)
        {
            return new V(a.X - b.X, a.Y - b.Y);
        }
        public static V operator *(V a, float b)
        {
            return new V(a.X * b, a.Y * b);
        }
        public static V operator /(V a, float b)
        {
            return new V(a.X / b, a.Y / b);
        }
        public static float Distance(V a, V b)
        {
            return (a - b).Length;
        }
        public static V Direction(float angle)
        {
            return new V((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
        public static float Angle(V v)
        {
            return (float)Math.Atan2(v.Y, v.X);
        }
        public static float Turn(float from, float to, float maximum)
        {
            float d = to - from;
            while (d > Math.PI)
                d -= (float)Math.PI * 2;
            while (d < -Math.PI)
                d += (float)Math.PI * 2;
            return from + Math.Max(-maximum, Math.Min(maximum, d));
        }
    }
}
