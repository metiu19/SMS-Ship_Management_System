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
        public enum States
        {
            Error = -1,
            Disabled = 0,
            Enabled = 1,
            GoingDown = 2,
            ComingUp = 3,
        }
        public static readonly Dictionary<States, string> StatesNames = new Dictionary<States, string>()
        {
            { States.Error, "Error" },
            { States.Disabled, "Disabled" },
            { States.Enabled, "Enabled" },
            { States.GoingDown, "Shutting Down" },
            { States.ComingUp, "Starting Up" },
        };

        public abstract class ShipModuleBase : IShipModule
        {
            protected readonly Program program;
            protected readonly Action<string> echo;
            protected readonly ScriptExceptions exceptionsManager;
            protected readonly MyIni ini = new MyIni();
            protected readonly Dictionary<string, string> requiredKeys = new Dictionary<string, string>()
            {
                {"props", "Properties" },
                {"up", "Startup" },
                {"down", "Shutdown"},
                {"defState", "Default State"},
                {"coolDelay", "Cooldown Delay"},
            };

            protected List<ModuleProperty> properties;
            protected List<ModuleAction> startupActions;
            protected List<ModuleAction> shutdownActions;
            protected string sectionName;
            protected double delayTarget;
            protected double cooldownDelay;
            protected int lastActionIndex;
            protected bool enabled;

            public string Name { get; protected set; }
            public States State { get; protected set; }


            public ShipModuleBase(Program program)
            {
                this.program = program;
                this.echo = program.Echo;
                this.exceptionsManager = program.ExceptionsManager;
            }

            public ShipModuleBase(MyIni ini, Program program)
            {
                this.program = program;
                this.echo = program.Echo;
                this.exceptionsManager = program.ExceptionsManager;
                this.ini = ini;
            }

            public void Init()
            {
                echo($"Init module: {Name}");
                // Defualt State
                this.enabled = ini.Get(sectionName, requiredKeys["defState"]).ToBoolean();
                if (enabled)
                    State = States.Enabled;
                else
                    State = States.Disabled;
                ForceState();

                // Cooldown delay
                this.cooldownDelay = ini.Get(sectionName, requiredKeys["coolDelay"]).ToDouble();

                // Register module properties
                echo("Register module properties");
                this.properties = ParseModuleProperties(ini.Get(sectionName, requiredKeys["props"]).ToString().Trim().Split('\n'));
                if (properties == null)
                {
                    exceptionsManager.AddMissingIniValueException(Name, requiredKeys["props"]);
                    return;
                }
                echo($"Registered module properties: {properties.Count}");

                // Register startup actions
                echo("Register startup actions");
                string[] rawStartupActions = ini.Get(sectionName, requiredKeys["up"]).ToString().Trim().Split('\n');
                startupActions = ParseModuleActions(rawStartupActions);
                if (startupActions == null)
                {
                    exceptionsManager.AddMissingIniValueException(Name, requiredKeys["up"]);
                    return;
                }
                if (!CheckModuleProperties(properties, startupActions))
                    exceptionsManager.AddModulePropertiesMissmatchException(Name);
                echo($"Registered startup properties: {startupActions.Count}");


                // Register shutdown actions
                echo("Register shutdown actions");
                string[] rawShutdownActions = ini.Get(sectionName, requiredKeys["down"]).ToString().Trim().Split('\n');
                shutdownActions = ParseModuleActions(rawShutdownActions);
                if (shutdownActions == null)
                {
                    exceptionsManager.AddMissingIniValueException(Name, requiredKeys["down"]);
                    return;
                }
                if (!CheckModuleProperties(properties, shutdownActions))
                    exceptionsManager.AddModulePropertiesMissmatchException(Name);
                echo($"Registered shutdown properties: {shutdownActions.Count}");
            }

            private List<ModuleProperty> ParseModuleProperties(string[] propertiesStrings)
            {
                if (string.IsNullOrEmpty(propertiesStrings[0]))
                    return null;

                List<ModuleProperty> properties = new List<ModuleProperty>();
                for (int x = 0; x < propertiesStrings.Length; x++)
                {
                    propertiesStrings[x] = propertiesStrings[x].Trim();
                    echo(propertiesStrings[x]);
                    properties.Add(new ModuleProperty(propertiesStrings[x], exceptionsManager));
                }
                return properties;
            }

            private List<ModuleAction> ParseModuleActions(string[] rawActions)
            {
                if (string.IsNullOrEmpty(rawActions[0]) || rawActions.Length == 0)
                    return null;

                List<ModuleAction> actions = new List<ModuleAction>();

                for (int x = 0; x < rawActions.Length; x++)
                {
                    string rawAction = rawActions[x].Trim();
                    echo(rawAction);
                    ModuleAction action;
                    if (!ModuleAction.TryParse(rawAction, out action))
                    {
                        exceptionsManager.AddModuleActionParseException(rawAction);
                        continue;
                    }
                    actions.Add(action);
                }

                return actions;
            }

            private bool CheckModuleProperties(List<ModuleProperty> properties, List<ModuleAction> actions)
            {
                bool ret = true;
                foreach (var action in actions)
                {
                    string propertyName = action.Property;
                    if (properties.Find(property => property.Name == propertyName) == null)
                        ret = false;
                }
                return ret;
            }


            public int CheckState()
            {
                if (program.FullLog) echo($"Checking state of module: {Name}");
                if (State == States.ComingUp && lastActionIndex == (startupActions.Count - 1) && program.Time > delayTarget)
                {
                    echo("Changing module state!");
                    State = States.Enabled;
                    enabled = true;
                    ForceState();
                    return 2;
                }
                else if (State == States.GoingDown && lastActionIndex == (shutdownActions.Count - 1) && program.Time > delayTarget)
                {
                    echo("Changing module state!");
                    State = States.Disabled;
                    delayTarget = program.Time + cooldownDelay;
                    return 3;
                }

                if (NeedsStateChange())
                {
                    ForceState();
                    return 1;
                }
                return 0;
            }

            protected abstract bool NeedsStateChange();

            protected abstract void ForceState();

            public int ToggleState()
            {
                if (program.Time < delayTarget)
                {
                    echo($"Action not available!\nWait: {delayTarget - program.Time} s");
                    return -1;
                }

                if (State == States.Disabled)
                    Startup();
                else if (State == States.Enabled)
                    Shutdown();
                else
                {
                    echo($"Action not available in the current state!\nCurrent State: {StatesNames[State]}");
                    return -2;
                }
                return 1;
            }

            public int SetState(bool state)
            {
                if (program.Time < delayTarget)
                {
                    echo($"Action not available!\nWait: {delayTarget - program.Time} s");
                    return -1;
                }

                if (State == States.Error || State == States.ComingUp || State == States.GoingDown)
                {
                    echo($"Action not available in the current state!\nCurrent State: {StatesNames[State]}");
                    return -2;
                }

                if (state && State == States.Disabled)
                {
                    Startup();
                    return 1;
                }
                else if (!state && State == States.Enabled)
                {
                    Shutdown();
                    return 2;
                }
                else
                {
                    echo($"Can't change module {Name} state - Current State: {StatesNames[State]}");
                    return -3;
                }
            }

            private void Startup()
            {
                echo($"Initializing startup procedure for module: {Name}");
                State = States.ComingUp;
                lastActionIndex = -1;
            }

            private void Shutdown()
            {
                echo($"Initializing shutdown procedure for module: {Name}");
                State = States.GoingDown;
                enabled = false;
                ForceState();
                lastActionIndex = -1;
            }

            public int TryFixError()
            {
                bool allGreen = true;
                foreach (var property in properties)
                    if (property.State != property.DefaultState)
                        allGreen = false;

                if (allGreen)
                {
                    echo("Restoring default state");
                    enabled = ini.Get(sectionName, requiredKeys["defState"]).ToBoolean();
                    if (enabled)
                        State = States.Enabled;
                    else
                        State = States.Disabled;
                    return 1;
                }
                echo("Could not restore default state!\nNot all properties were reseted to the default value");
                return 0;
            }


            public Dictionary<string, bool> GetProperties()
            {
                Dictionary<string, bool> properties = new Dictionary<string, bool>();
                this.properties.ForEach(p => properties.Add(p.Name, p.State));
                return properties;
            }

            public bool? GetProperty(string propertyName)
            {
                return properties.Find(p => p.Name == propertyName)?.State;
            }

            public int ToggleProperty(string propertyName)
            {
                if (program.Time < delayTarget)
                {
                    echo($"Action not available!\nWait: {delayTarget - program.Time} s");
                    return -1;
                }

                if (!(State == States.Error || State == States.GoingDown || State == States.ComingUp))
                {
                    echo($"Action not available in the current state!\nState: {StatesNames[State]}");
                    return -2;
                }

                var property = properties.Find(p => p.Name == propertyName);
                if (property == null)
                    return -3;

                return SetProperty(property, !property.State);
            }

            public int SetProperty(string propertyName, bool state)
            {
                if (program.Time < delayTarget)
                {
                    echo($"Action not available!\nWait: {delayTarget - program.Time} s");
                    return -1;
                }

                if (!(State == States.Error || State == States.GoingDown || State == States.ComingUp))
                {
                    echo($"Action not available in the current state!\nState: {StatesNames[State]}");
                    return -2;
                }

                var property = properties.Find(p => p.Name == propertyName);
                if (property == null)
                    return -3;

                
                return SetProperty(property, state);
            }

            private int SetProperty(ModuleProperty property, bool propertyState)
            {
                if (State == States.ComingUp)
                {
                    ModuleAction nextAction = startupActions[++lastActionIndex];
                    if (nextAction.Property != property.Name)
                    {
                        echo($"Wrong property! Sequence not respected!");
                        State = States.Error;
                        return -4;
                    }
                    if (nextAction.NeededState != propertyState)
                    {
                        echo($"Wrong state! Sequence not respected!");
                        State = States.Error;
                        return -5;
                    }
                }
                else if (State == States.GoingDown)
                {
                    ModuleAction nextAction = shutdownActions[++lastActionIndex];
                    if (nextAction.Property != property.Name)
                    {
                        echo($"Wrong property! Sequence not respected!");
                        State = States.Error;
                        return -4;
                    }
                    if (nextAction.NeededState != propertyState)
                    {
                        echo($"Wrong state! Sequence not respected!");
                        State = States.Error;
                        return -5;
                    }
                }

                if (property.State)
                    delayTarget = program.Time + property.ShutdownDelay;
                else
                    delayTarget = program.Time + property.StartupDelay;

                property.State = propertyState;
                return 1;
            }
        }

        public class BlockModule : ShipModuleBase
        {
            private readonly IMyFunctionalBlock moduleBlock;

            /// <summary>
            /// Register a single Block as a Ship Module
            /// </summary>
            /// <param name="block">The block to be registered</param>
            /// <param name="program">Reference to the Program Object</param>
            public BlockModule(IMyFunctionalBlock block, Program program) : base(program)
            {
                this.Name = block.CustomName;
                this.sectionName = "SMS - Module";
                this.moduleBlock = block;

                echo($"Registring block module: {Name}");

                MyIniParseResult result;
                if (!ini.TryParse(block.CustomData, out result))
                    exceptionsManager.AddMyIniParseException(result);

                echo("Searching keys for block module");
                List<MyIniKey> keys = new List<MyIniKey>();
                ini.GetKeys(sectionName, keys);
                foreach (MyIniKey key in keys)
                    echo($"Found key: {key.Name}");

                foreach (string keyName in requiredKeys.Values)
                    if (keys.Find(key => key.Name == keyName) == default(MyIniKey))
                        exceptionsManager.AddMissingIniKeyException(Name, keyName);
            }


            protected override bool NeedsStateChange()
            {
                return moduleBlock.Enabled != enabled;
            }

            protected override void ForceState()
            {
                echo($"Forcing state to: {(enabled ? "Enabled" : "Disabled")}");
                moduleBlock.Enabled = enabled;
            }
        }

        public class GroupModule : ShipModuleBase
        {
            private readonly List<IMyFunctionalBlock> moduleBlocks = new List<IMyFunctionalBlock>();

            /// <summary>
            /// Register a Block Group as a Ship Module
            /// </summary>
            /// <param name="group">The group to be registered</param>
            /// <param name="ini">Reference to the group config</param>
            /// <param name="program">Reference to the Program Object</param>
            public GroupModule(IMyBlockGroup group, MyIni ini, Program program) : base(ini, program)
            {
                this.Name = group.Name;
                this.sectionName = Name;
                group.GetBlocksOfType(this.moduleBlocks);

                echo($"Registring group module: {Name}");
                echo($"Found {moduleBlocks.Count} blocks");

                echo("Searching keys");
                List<MyIniKey> keys = new List<MyIniKey>();
                ini.GetKeys(Name, keys);
                foreach (MyIniKey key in keys)
                    echo($"Found key: {key.Name}");

                foreach (string keyName in requiredKeys.Values)
                    if (keys.Find(key => key.Name == keyName) == default(MyIniKey))
                        exceptionsManager.AddMissingIniKeyException(Name, keyName);
            }


            protected override bool NeedsStateChange()
            {
                bool ret = false;
                foreach (IMyFunctionalBlock block in moduleBlocks)
                    if (block.Enabled != enabled)
                        ret = true;

                return ret;
            }

            protected override void ForceState()
            {
                echo($"Forcing state to: {(enabled ? "Enabled" : "Disabled")}");
                foreach (IMyFunctionalBlock block in moduleBlocks)
                    block.Enabled = enabled;
            }
        }
    }
}
