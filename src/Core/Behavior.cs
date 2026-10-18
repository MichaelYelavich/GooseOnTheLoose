using System;
using System.IO;
using System.Linq;
namespace SecondHonk
{
    public sealed class Behavior
    {
        public float PrankMinSeconds = 20, PrankMaxSeconds = 45, FootprintSeconds = 10, ToyApproachSpeed = 200, NudgeSpeed = 260;
        public int TrailSteps = 20, MudWeight = 2, NoteWeight = 2, PictureWeight = 2, ChaseWeight = 1, ToyWeight = 1;
        public void Validate()
        {
            float[] values = { PrankMinSeconds, PrankMaxSeconds, FootprintSeconds, ToyApproachSpeed, NudgeSpeed };
            if (values.Any(v => float.IsNaN(v) || float.IsInfinity(v)) || PrankMinSeconds < 5 || PrankMaxSeconds < PrankMinSeconds || PrankMaxSeconds > 3600 || FootprintSeconds < 1 || FootprintSeconds > 60 || TrailSteps < 0 || TrailSteps > 100 || ToyApproachSpeed < 20 || ToyApproachSpeed > 400 || NudgeSpeed < 0 || NudgeSpeed > 600 || new[] { MudWeight, NoteWeight, PictureWeight, ChaseWeight, ToyWeight }.Any(i => i < 0 || i > 20))
                throw new InvalidDataException("Preset values are outside the documented ranges.");
        }
    }
}
