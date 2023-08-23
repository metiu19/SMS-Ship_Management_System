using Microsoft.Win32;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
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
    partial class Program : MyGridProgram
    {
        readonly bool forceState;
        readonly bool debug;
        public const string ScriptName = "[SMS] Ship Management System";
        public const string ScriptVersion = "V0.1.0";
        public MyIni Ini = new MyIni();
        public DebugLogs DebugLogsHelper;
        public ScriptExceptions ExceptionsManager;
        public double Time = 0;
        public StateMachine CoRoutines;
        private readonly List<IShipModule> ShipModules = new List<IShipModule>();
        private readonly MyCommandLine CommandLine = new MyCommandLine();
        private readonly Dictionary<string, Action> Commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);

        public Program()
        {
            // Init Debug LCD 
            IMyTextPanel LogsScreen = GridTerminalSystem.GetBlockWithName("LogsScreen") as IMyTextPanel;
            DebugLogsHelper = new DebugLogs(LogsScreen, ScriptName, ScriptVersion);
            // Echo on screens rather than on PB's detail area
            Echo = DebugLogsHelper.Append; // Comment this line for default Echo

            // Init custom Exceptions manager
            Echo("Init custom Exceptions manager");
            ExceptionsManager = new ScriptExceptions(DebugLogsHelper);

            // Get configs from PB CustomData
            Echo("Get configs from PB CustomData");
            MyIniParseResult result;
            if (!Ini.TryParse(Me.CustomData, out result))
                ExceptionsManager.AddMyIniParseException(result);

            // General Configs
            forceState = Ini.Get("SMS - General", "Force State").ToBoolean(true);
            debug = Ini.Get("SMS - General", "Debug").ToBoolean(false);
            DebugLogsHelper.Enable = debug;

            // Cache grid blocks whit script config
            Echo("Cache single block modules");
            List<IMyFunctionalBlock> singleBlockModules = new List<IMyFunctionalBlock>();
            GridTerminalSystem.GetBlocksOfType(singleBlockModules, block => MyIni.HasSection(block.CustomData, "SMS - Module"));

            Echo("Cache block group modules");
            List<string> sections = new List<string>();
            Ini.GetSections(sections);
            sections.Remove("SMS - General");
            List<IMyBlockGroup> groupModules = new List<IMyBlockGroup>();
            foreach (string section in sections)
                groupModules.Add(GridTerminalSystem.GetBlockGroupWithName(section));

            // Register Ship Modules
            Echo("Registering Single Block Modules");
            foreach (IMyFunctionalBlock block in singleBlockModules)
                ShipModules.Add(new BlockModule(block, this));
            Echo($"Registered {ShipModules.Count} Single Block Modules");
            Echo("Registering Block Group Modules");
            foreach (IMyBlockGroup group in groupModules)
                ShipModules.Add(new GroupModule(group, Ini, this));
            Echo($"{ShipModules.Count} Modules Registered!");

            // Throw any exceptions
            ExceptionsManager.ThrowExceptions();

            // Setup state machine
            CoRoutines = new StateMachine(this);
            CoRoutines.AddToSerialQueue(InitModulesState());
            Runtime.UpdateFrequency = UpdateFrequency.Once;
            if (forceState)
                Runtime.UpdateFrequency |= UpdateFrequency.Update10;

            // Setup commands
            Commands.Add("module", HandleModuleCommands);
            Commands.Add("property", HandleModulePropertyCommands);
            Commands.Add("clear", DebugLogsHelper.Clear);
            Commands.Add("check", CheckModulesState);

            Echo("This is fine!");
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo("Main runned!");
            Time += Runtime.TimeSinceLastRun.TotalSeconds;

            if ((updateSource & UpdateType.Once) > 0)
                CoRoutines.Tick();

            if ((updateSource & UpdateType.Update10) > 0)
                CheckModulesState();

            // Handle Commands
            if ((updateSource & (UpdateType.Script | UpdateType.Terminal | UpdateType.Trigger)) > 0)
            {
                if (CommandLine.TryParse(argument))
                {
                    Action commandAction;
                    string commandName = CommandLine.Argument(0);

                    if (commandName == null)
                    {
                        Echo("Command is empty");
                    }
                    else if (Commands.TryGetValue(commandName, out commandAction))
                    {
                        Echo($"Runned Command: {commandName}");
                        commandAction();
                    }
                    else
                    {
                        Echo($"Uknow command: {commandName}");
                    }
                }
            }
        }

        // Handle Module Commands
        private void HandleModuleCommands()
        {
            if (CommandLine.ArgumentCount < 3)
            {
                Echo("Command missing arguments");
                return;
            }

            string subCommand = CommandLine.Argument(1);
            Echo($"Runned sub-command: {subCommand}");
            switch (subCommand)
            {
                case "get":
                case "list":
                    GetModuleState();
                    break;
                case "toggle":
                    ToggleModuleState();
                    break;
                case "on":
                case "set":
                    SetModuleState(true);
                    break;
                case "off":
                case "reset":
                    SetModuleState(false);
                    break;
                case "fix":
                case "repaire":
                    FixModuleState();
                    break;
                default:
                    Echo("Unknow sub-command");
                    break;
            }
        }

        private void GetModuleState()
        {
            string name = CommandLine.Argument(2);
            Echo($"Module Name: {name}");
            IShipModule module = ShipModules.Find(m => m.Name == name);
            if (module == null)
            {
                Echo("Module not found!");
                return;
            }
            Echo($"Module state: {StatesNames[module.State]}");
        }

        private void ToggleModuleState()
        {
            string name = CommandLine.Argument(2);
            Echo($"Module Name: {name}");
            IShipModule module = ShipModules.Find(m => m.Name == name);
            if (module == null )
            {
                Echo("Module not found!");
                return;
            }
            module.ToggleState();
            Echo($"Module state: {StatesNames[module.State]}");
        }

        private void SetModuleState(bool state)
        {
            string name = CommandLine.Argument(2);
            Echo($"Module name: {name}");
            IShipModule module = ShipModules.Find(m => m.Name == name);
            if (module == null)
            {
                Echo("Module not found!");
                return;
            }
            module.SetState(state);
            Echo($"Module state: {StatesNames[module.State]}");
        }

        private void FixModuleState()
        {
            string name = CommandLine.Argument(2);
            Echo($"Module name: {name}");
            IShipModule module = ShipModules.Find(m => m.Name == name);
            if (module == null )
            {
                Echo("Module not found!");
                return;
            }
            module.TryFixError();
            Echo($"Module state: {StatesNames[module.State]}");
        }

        // Handle Properties Commands
        private void HandleModulePropertyCommands()
        {
            if (CommandLine.ArgumentCount < 4)
            {
                Echo("Command missing arguments");
                return;
            }

            string subCommand = CommandLine.Argument(1);
            Echo($"Runned sub-command: {subCommand}");
            switch (subCommand)
            {
                case "get":
                case "list":
                    GetModuleProperty();
                    break;
                case "toggle":
                    ToggleModuleProperty();
                    break;
                case "on":
                case "set":
                    SetModuleProperty(true);
                    break;
                case "off":
                case "reset":
                    SetModuleProperty(false);
                    break;
                default:
                    Echo("Unknow sub-command");
                    break;
            }
        }

        private void GetModuleProperty()
        {
            string moduleName = CommandLine.Argument(2);
            Echo($"Module name: {moduleName}");
            IShipModule module = ShipModules.Find(m => m.Name == moduleName);
            if (module == null)
            {
                Echo("Module not found!");
                return;
            }
            string propertyName = CommandLine.Argument(3);
            Echo($"Property name: {propertyName}");
            Echo($"Property state: {(module.GetProperty(propertyName) ? "Enabled" : "Disabled")}");
        }

        private void ToggleModuleProperty()
        {
            string moduleName = CommandLine.Argument(2);
            Echo($"Module name: {moduleName}");
            IShipModule module = ShipModules.Find(m => m.Name == moduleName);
            if (module == null)
            {
                Echo("Module not found!");
                return;
            }
            string propertyName = CommandLine.Argument(3);
            Echo($"Property name: {propertyName}");
            module.ToggleProperty(propertyName);
            Echo($"Property state: {module.GetProperty(propertyName)}");
        }

        private void SetModuleProperty(bool state)
        {
            string moduleName = CommandLine.Argument(2);
            Echo($"Module name: {moduleName}");
            IShipModule module = ShipModules.Find(m => m.Name == moduleName);
            if (module == null)
            {
                Echo("Module not found!");
                return;
            }
            string propertyName = CommandLine.Argument(3);
            Echo($"Property name: {propertyName}");
            module.SetProperty(propertyName, state);
            Echo($"Property state: {module.GetProperty(propertyName)}");
        }

        // Recurrent Functions
        private IEnumerator<bool> InitModulesState()
        {
            Echo("Init Module State");
            foreach (IShipModule Module in ShipModules)
            {
                Module.Init();
                yield return true;
            }
            ExceptionsManager.ThrowExceptions();
        }

        private void CheckModulesState()
        {
            foreach (IShipModule module in ShipModules)
                CoRoutines.AddToSerialQueue(CheckModuleState(module));
        }

        private IEnumerator<bool> CheckModuleState(IShipModule module)
        {
            module.CheckState();
            yield return true;
        }
    }
}
