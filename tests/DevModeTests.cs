using System;
using System.Linq;

namespace SecondHonk
{
    static class DevModeTests
    {
        public static void Run(Content content, Action<bool, string> check)
        {
            check(CommandRegistry.Find("dev") != null && CommandRegistry.Find("dev").Id == 116, "The dev command keeps the shared registry pattern with its own id");
            using (var plain = new DesktopSession(new SaveData { Sound = false }, content, "", true))
            using (var flagged = new DesktopSession(new SaveData { Sound = false }, content, "dev", true))
            {
                check(!plain.DevMode && plain.dev == null, "Launching without --dev leaves dev mode and its overlay off");
                check(flagged.DevMode && flagged.dev != null && !flagged.dev.IsDisposed, "Launching with --dev enables dev mode and its overlay");
                var panels = DevReadout.DevPanel(flagged.World, 59.9f, 16.7f);
                string text = string.Join("\n", panels.ToArray());
                check(text.Contains("fps 59.9") && text.Contains("frame 16.70 ms") && text.Contains("objects 0"), "Performance panel reads fps, frame time and the on-screen object count");
                check(text.Contains("bond ") && text.Contains("scenes ") && text.Contains("accept +") && text.Contains("partner-note"), "Bond panel reads the value, accruals and warmth factors");
                check(text.Contains("mode Normal") && text.Contains("scene ") && text.Contains("broker ") && text.Contains("cursor "), "State panel reads the scene, broker state and mode");
            }

            var world = new World(new SaveData { Scale = 1, Companion = true }, content, 42);
            world.Resize(1280, 752);
            world.CreditBond(SceneKind.BallPass);
            world.CreditBond(SceneKind.BallPass);
            check(world.BondLog.Count == 2 && world.BondLog[0].Contains("BallPass"), "Completed scenes record recent accrual events for display");
            check(world.Data.Bond > 0, "Accrual recording leaves the bond value itself untouched");
            for (int i = 0; i < 10; i++)
                world.CreditBond(SceneKind.Rest);
            check(world.BondLog.Count == 8, "The accrual log stays bounded");
        }
    }
}
