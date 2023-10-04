using Sandbox.ModAPI;
using SMS.TouchAPI;
using VRage.Game.Components;

namespace SMS
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SMSMod : MySessionComponentBase
    {
        public static SMSMod Instance;
        public TouchUiKit TouchAPI { get; private set; }

        public override void LoadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            Instance = this;
            TouchAPI = new TouchUiKit();
            TouchAPI.Load();
        }

        protected override void UnloadData()
        {
            TouchAPI?.Unload();
            Instance = null;
        }
    }
}
