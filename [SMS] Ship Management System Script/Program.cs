﻿using Microsoft.Win32;
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
        public readonly bool FullLog;
        public const string ScriptName = "[SMS] Ship Management System";
        public const string ScriptVersion = "V0.1.7";
        public MyIni Ini = new MyIni();
        public DebugLogs DebugLogsHelper;
        public ScriptExceptions ExceptionsManager;
        public double Time = 0;
        public StateMachine CoRoutines;
        private readonly List<IShipModule> ShipModules = new List<IShipModule>();
        private readonly MyCommandLine CommandLine = new MyCommandLine();
        private readonly Dictionary<string, Action> Commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
        private readonly SMSAPI tssAPI;

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

            // Init TSS API
            tssAPI = new SMSAPI(this);
            if (tssAPI == null)
                ExceptionsManager.AddException("TSSAPIException: Couldn't init TSS API!");

            // General Configs
            forceState = Ini.Get("SMS - General", "Force State").ToBoolean(true);
            debug = Ini.Get("SMS - General", "Debug").ToBoolean(false);
            DebugLogsHelper.Enable = debug;
            FullLog = forceState & !debug;

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
            {
                ShipModules.Add(new BlockModule(block, this));
            }
            Echo($"Registered {ShipModules.Count} Single Block Modules");
            Echo("Registering Block Group Modules");
            foreach (IMyBlockGroup group in groupModules)
            {
                ShipModules.Add(new GroupModule(group, Ini, this));
            }
            Echo($"{ShipModules.Count} Modules Registered!");

            // Throw any exceptions
            ExceptionsManager.ThrowExceptions();

            // Register PB with TSS API
            tssAPI.ResetApp();
            tssAPI.RegisterPB();

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
            if (FullLog) Echo("Main runned!");
            Time += Runtime.TimeSinceLastRun.TotalSeconds;

            if ((updateSource & UpdateType.Once) > 0)
                CoRoutines.Tick();

            if ((updateSource & UpdateType.Update10) > 0)
                CheckModulesState();

            // Handle Commands
            if ((updateSource & (UpdateType.Script | UpdateType.Terminal | UpdateType.Trigger | UpdateType.Mod)) > 0)
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

            tssAPI.SetCommandOutput("", false, CommandLine.Argument(2), CommandLine.Argument(3));

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
            if (module == null)
            {
                Echo("Module not found!");
                return;
            }
            switch (module.ToggleState())
            {
                case 1:
                    Echo("Module state toggled");
                    Echo($"Module state: {StatesNames[module.State]}");
                    tssAPI.SetModuleState(name, (int)module.State);
                    tssAPI.SetCommandOutput("Module state toggled", false, name, CommandLine.Argument(3));
                    break;
                case -1:
                    tssAPI.SetCommandOutput("Action not available! Please wait!", true, name, CommandLine.Argument(3));
                    break;
                case -2:
                    tssAPI.SetCommandOutput("Action not available in the current state!", true, name, CommandLine.Argument(3));
                    break;
            }
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
            switch (module.SetState(state))
            {
                case 1:
                case 2:
                    tssAPI.SetCommandOutput("Module state set", false, name, CommandLine.Argument(3));
                    Echo($"Module state: {StatesNames[module.State]}");
                    tssAPI.SetModuleState(name, (int)module.State);
                    break;
                case -1:
                    tssAPI.SetCommandOutput("Action not available! Please wait!", true, name, CommandLine.Argument(3));
                    break;
                case -2:
                    tssAPI.SetCommandOutput("Action not available in the current state!", true, name, CommandLine.Argument(3));
                    break;
                case -3:
                    tssAPI.SetCommandOutput("Can't change module state", true, name, CommandLine.Argument(3));
                    break;
            }
        }

        private void FixModuleState()
        {
            string name = CommandLine.Argument(2);
            Echo($"Module name: {name}");
            IShipModule module = ShipModules.Find(m => m.Name == name);
            if (module == null)
            {
                Echo("Module not found!");
                return;
            }
            switch (module.TryFixError())
            {
                case 0:
                    Echo("Module error state not fixed");
                    tssAPI.SetCommandOutput("Module error state not fixed", true, name, CommandLine.Argument(3));
                    break;
                case 1:
                    Echo($"Module state: {StatesNames[module.State]}");
                    tssAPI.SetModuleState(name, (int)module.State);
                    tssAPI.SetCommandOutput("Module error state fixed", false, name, CommandLine.Argument(3));
                    break;
            }
        }

        // Handle Properties Commands
        private void HandleModulePropertyCommands()
        {
            if (CommandLine.ArgumentCount < 4)
            {
                Echo("Command missing arguments");
                return;
            }

            tssAPI.SetCommandOutput("", false, CommandLine.Argument(2), CommandLine.Argument(4));

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
            var state = module.GetProperty(propertyName);
            if (state == null)
            {
                Echo("Property not found!");
                return;
            }
            Echo($"Property state: {((bool)state ? "Enabled" : "Disabled")}");
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
            HandleModulePropertyReturn(module.ToggleProperty(propertyName), module, propertyName);
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
            HandleModulePropertyReturn(module.SetProperty(propertyName, state), module, propertyName);
        }

        private void HandleModulePropertyReturn(int returnVal, IShipModule module, string propertyName)
        {
            switch (returnVal)
            {
                case 1:
                    tssAPI.SetCommandOutput("Property state changed", false, module.Name, CommandLine.Argument(4));
                    tssAPI.SetModuleState(module.Name, (int)module.State);
                    Echo($"Property state: {module.GetProperty(propertyName)}");
                    tssAPI.SetPropertyState(module.Name, propertyName, (bool)module.GetProperty(propertyName));
                    break;
                case -1:
                    tssAPI.SetCommandOutput("Action not available! Please wait!", true, module.Name, CommandLine.Argument(4));
                    break;
                case -2:
                    tssAPI.SetCommandOutput("Action not available in the current state!", true, module.Name, CommandLine.Argument(4));
                    break;
                case -3:
                    Echo($"Error: Property {propertyName} not found!");
                    break;
                case -4:
                    tssAPI.SetModuleState(module.Name, (int)module.State);
                    tssAPI.SetCommandOutput("Wrong property! Sequence not respected!", true, module.Name, CommandLine.Argument(4));
                    break;
                case -5:
                    tssAPI.SetModuleState(module.Name, (int)module.State);
                    tssAPI.SetCommandOutput("Wrong state! Sequence not respected!", true, module.Name, CommandLine.Argument(4));
                    break;
            }
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
            foreach (IShipModule module in ShipModules)
            {
                tssAPI.RegisterModule(module.Name, (int)module.State, module.GetProperties());
                yield return true;
            }
            if (!tssAPI.LoadModules())
            {
                ExceptionsManager.AddException("TSSAPIException: Modules load failed!");
                ExceptionsManager.ThrowExceptions();
            }
        }

        private void CheckModulesState()
        {
            foreach (IShipModule module in ShipModules)
                CoRoutines.AddToSerialQueue(CheckModuleState(module));
        }

        private IEnumerator<bool> CheckModuleState(IShipModule module)
        {
            switch (module.CheckState())    // Skipping state 0 bc nothing happend
            {
                case 1:
                    tssAPI.SetCheckOutput("Module state forced!", module.Name);
                    break;
                case 2:
                    tssAPI.SetModuleState(module.Name, (int)module.State);
                    tssAPI.SetCheckOutput("Module enabled!", module.Name);
                    break;
                case 3:
                    tssAPI.SetModuleState(module.Name, (int)module.State);
                    tssAPI.SetCheckOutput("Module disabled!", module.Name);
                    break;
            }

            yield return true;
        }
    }
}
