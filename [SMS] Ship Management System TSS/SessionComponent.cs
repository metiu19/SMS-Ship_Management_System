using Sandbox.ModAPI;
using SMS.TouchAPI;
using System.Collections.Generic;
using VRage.Game.Components;

namespace SMS
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public partial class SMSMod : MySessionComponentBase
    {
        public static SMSMod Instance;
        public TouchUiKit TouchAPI { get; private set; }
        public List<SMSTSS> TSSInstances { get; private set; }
        public PBInterface PBAPI { get; private set; }

        public override void LoadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            Instance = this;
            TouchAPI = new TouchUiKit();
            TouchAPI.Load();

            TSSInstances = new List<SMSTSS>();

            PBAPI = new PBInterface("SMS_PB-API");
            SetupAPI();
        }

        protected override void UnloadData()
        {
            TouchAPI?.Unload();
            PBAPI?.Dispose();
            Instance = null;
        }
    }
}
