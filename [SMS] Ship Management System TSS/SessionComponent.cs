using Sandbox.ModAPI;
using SMS.API;
using VRage.Game.Components;

namespace SMS
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SMSMod : MySessionComponentBase
    {
        public static SMSMod Instance;
        public TouchUiKit API { get; private set; }

        public override void LoadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            Instance = this;
            API = new TouchUiKit();
            API.Load();
        }

        protected override void UnloadData()
        {
            API?.Unload();
            Instance = null;
        }
    }
}
