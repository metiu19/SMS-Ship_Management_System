using Sandbox.ModAPI;
using SMS.TouchAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace SMS
{
    public class SMSApp : TouchApp
    {
        readonly string version = "v0.0.0";

        public SMSApp(IMyCubeBlock block, IMyTextSurface surface) : base(block, surface)
        {
            DefaultBg = true;

            var windowBar = new WindowBar($"[SMS] Ship Management System - [{version}]");
            windowBar.BgColor = Color.DarkCyan;
            windowBar.Label.TextColor = Color.White;

            var window = new View();
            window.Border = new Vector4(3);
            window.Padding = new Vector4(9);

            AddChild(windowBar);
            AddChild(window);
        }
    }
}
