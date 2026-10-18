using System;
namespace SecondHonk
{
    public sealed class ShutdownClock
    {
        public float Held, EffectTime; public bool Animating, Finished;
        public bool Advance(bool escape, float dt, bool reduced)
        {
            if (Finished)
                return false;
            if (Animating)
            {
                EffectTime += Math.Max(0, dt);
                Finished = EffectTime >= (reduced ? .25f : .8f);
                return false;
            }
            Held = escape ? Held + Math.Max(0, dt) : 0;
            if (Held >= 5)
            {
                Held = 5;
                Animating = true;
                return true;
            }
            return false;
        }
    }
}
