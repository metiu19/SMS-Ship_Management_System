using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class SMSAPI
        {
            private Program program;
            private IMyProgrammableBlock pb;
            private readonly bool enabled = true;

            public SMSAPI(Program program)
            {
                this.program = program;
                this.pb = program.Me;

                var delegates = pb.GetProperty("SMS_PB-API")?.As<IReadOnlyDictionary<string, Delegate>>()?.GetValue(pb);
                if (delegates == null)
                {
                    enabled = false;
                    return;
                }

                AssignMethod(out resetApp, delegates["resetApp"]);
                AssignMethod(out registerPB, delegates["registerPB"]);
                AssignMethod(out registerModule, delegates["registerModule"]);
                AssignMethod(out loadModules, delegates["loadModules"]);
                AssignMethod(out setModuleState, delegates["setModuleState"]);
                AssignMethod(out setPropertyState, delegates["setPropertyState"]);
                AssignMethod(out setCommandOutput, delegates["setCommandOutput"]);
                AssignMethod(out setCheckOutput, delegates["setCheckOutput"]);
            }

            private void AssignMethod<T>(out T field, object method) => field = (T)method;

            public void ResetApp() => resetApp?.Invoke(pb);
            readonly Action<IMyProgrammableBlock> resetApp;

            public void RegisterPB()
            {
                if (enabled)
                    registerPB?.Invoke(pb);
            }

            readonly Action<IMyProgrammableBlock> registerPB;

            public void RegisterModule(string moduleName, int moduleState, Dictionary<string, bool> properties)
            {
                if (enabled)
                    registerModule?.Invoke(pb, moduleName, moduleState, properties);
            }
            readonly Action<IMyProgrammableBlock, string, int, Dictionary<string, bool>> registerModule;

            public bool LoadModules()
            {
                if (!enabled)
                    return true;
                return loadModules?.Invoke(pb) ?? false;
            }
            readonly Func<IMyProgrammableBlock, bool> loadModules;

            public void SetModuleState(string moduleName, int moduleState)
            {
                if (enabled)
                    setModuleState?.Invoke(pb, moduleName, moduleState);
            }
            readonly Action<IMyProgrammableBlock, string, int> setModuleState;

            public void SetPropertyState(string moduleName, string propertyName, bool propertyState)
            {
                if (enabled)
                    setPropertyState?.Invoke(pb, moduleName, propertyName, propertyState);
            }
            readonly Action<IMyProgrammableBlock, string, string, bool> setPropertyState;

            public void SetCommandOutput(string commandOutput, bool isError, string moduleName, string appID)
            {
                if (enabled)
                    setCommandOutput?.Invoke(pb, commandOutput, isError, moduleName, appID);
            }
            readonly Action<IMyProgrammableBlock, string, bool, string, string> setCommandOutput;

            public void SetCheckOutput(string checkOutput, string moduleName)
            {
                if (enabled)
                    setCheckOutput?.Invoke(pb, checkOutput, moduleName);
            }
            readonly Action<IMyProgrammableBlock, string, string> setCheckOutput;
        }
    }
}
