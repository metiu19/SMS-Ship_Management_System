using System;
using System.Collections.Generic;
using VRage;
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;

namespace SMS
{
    public partial class SMSMod
    {
        void SetupAPI()
        {
            PBAPI.AddMethod("resetApp", new Action<IMyProgrammableBlock>(ResetApp));
            PBAPI.AddMethod("registerPB", new Action<IMyProgrammableBlock>(RegisterPB));
            PBAPI.AddMethod("registerModule", new Action<IMyProgrammableBlock, string, int, Dictionary<string, bool>>(RegisterModule));
            PBAPI.AddMethod("loadModules", new Func<IMyProgrammableBlock, bool>(LoadModules));
            PBAPI.AddMethod("setModuleState", new Action<IMyProgrammableBlock, string, int>(SetModuleState));
            PBAPI.AddMethod("setPropertyState", new Action<IMyProgrammableBlock, string, string, bool>(SetPropertyState));
            PBAPI.AddMethod("setCommandOutput", new Action<IMyProgrammableBlock, string, bool, string, string>(SetCommandOutput));
            PBAPI.AddMethod("setCheckOutput", new Action<IMyProgrammableBlock, string, string>(SetCheckOutput));
        }

        // Utils
        bool CheckCubeGrid(SMSTSS tssInstance, IMyProgrammableBlock pb)
        {
            return tssInstance.Block.CubeGrid == pb.CubeGrid;
        }

        bool CheckPB(SMSTSS tssInstance, IMyProgrammableBlock pb)
        {
            return tssInstance.App.PB == (Sandbox.ModAPI.IMyProgrammableBlock)pb;
        }

        // API Methods

        void ResetApp(IMyProgrammableBlock pb)
        {
            foreach (var tssInstance in TSSInstances)
            {
                if (!CheckCubeGrid(tssInstance, pb) && !CheckPB(tssInstance, pb))
                    continue;
                tssInstance.App.ResetApp();
            }
        }

        void RegisterPB(IMyProgrammableBlock pb)
        {
            foreach (var tssInstance in TSSInstances)
            {
                if (!CheckCubeGrid(tssInstance, pb))
                    continue;
                if (tssInstance.App.PB == null)
                    tssInstance.App.PB = (Sandbox.ModAPI.IMyProgrammableBlock)pb;
            }
        }

        void RegisterModule(IMyProgrammableBlock pb, string moduleName, int moduleState, Dictionary<string, bool> properties)
        {
            foreach (var tssInstance in TSSInstances)
            {
                if (!CheckCubeGrid(tssInstance, pb) || !CheckPB(tssInstance, pb))
                    continue;

                tssInstance.App.RegisterModule(moduleName, moduleState, properties);
            }
        }

        bool LoadModules(IMyProgrammableBlock pb)
        {
            bool ret = true;
            foreach (var tssInstance in TSSInstances)
            {
                if (!CheckCubeGrid(tssInstance, pb) || !CheckPB(tssInstance, pb))
                    continue;

                ret &= tssInstance.App.LoadModules();
            }
            return ret;
        }

        void SetModuleState(IMyProgrammableBlock pb, string moduleName, int moduleState)
        {
            foreach (var tssInstance in TSSInstances)
            {
                if (!CheckCubeGrid(tssInstance, pb) || !CheckPB(tssInstance, pb))
                    continue;

                var module = tssInstance.App.Modules.Find(m => m.Name == moduleName);
                module?.SetModuleState(moduleState);
            }
        }

        void SetPropertyState(IMyProgrammableBlock pb, string moduleName, string propertyName, bool propertyState)
        {
            foreach (var tssInstance in TSSInstances)
            {
                if (!CheckCubeGrid(tssInstance, pb) || !CheckPB(tssInstance, pb))
                    continue;

                var module = tssInstance.App.Modules.Find(m => m.Name == moduleName);
                module?.SetPropertyState(propertyName, propertyState);
            }
        }

        void SetCommandOutput(IMyProgrammableBlock pb, string commandOutput, bool error, string moduleName, string appID)
        {
            foreach (var tssInstance in TSSInstances)
            {
                if (!CheckCubeGrid(tssInstance, pb) || !CheckPB(tssInstance, pb))
                    continue;
                if (tssInstance.App.ID != appID)
                    continue;
                var module = tssInstance.App.Modules.Find(m => m.Name == moduleName);
                module?.UpdateCommandOutputLabel(commandOutput, error);
            }
        }

        void SetCheckOutput(IMyProgrammableBlock pb, string checkOutput, string moduleName)
        {
            foreach (var tssInstance in TSSInstances)
            {
                if (!CheckCubeGrid(tssInstance, pb) || !CheckPB(tssInstance, pb))
                    continue;
                var module = tssInstance.App.Modules.Find(m => m.Name == moduleName);
                module?.UpdateCommandOutputLabel(checkOutput, false);
            }
        }
    }
}
