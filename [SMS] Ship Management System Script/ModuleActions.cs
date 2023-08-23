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
        public class ModuleAction
        {
            public bool NeededState { get; private set; }
            public string Property { get; private set; }

            public ModuleAction() { }
            private ModuleAction(string propertyName, bool neededState)
            {
                this.NeededState = neededState;
                this.Property = propertyName;
            }

            public static bool TryParse(string instruction, out ModuleAction action)
            {
                action = null;
                if (string.IsNullOrEmpty(instruction))
                    return false;

                string[] args = instruction.Trim().Split(' ');
                if (args.Length < 2)
                    return false;

                bool state;
                if (!TryParseState(args[0], out state))
                    return false;

                action = new ModuleAction(args[1], state);

                return true;
            }
        }

        public class ModuleProperty
        {
            public readonly string Name;
            public readonly bool DefaultState;
            public readonly double StartupDelay;
            public readonly double ShutdownDelay;
            public bool State;

            public ModuleProperty(string property, ScriptExceptions exceptionsManager)
            {
                string[] prop = property.Split(' ');
                if (prop.Length != 4)
                {
                    exceptionsManager.AddModulePropertyException(property);
                    return;
                }

                bool defVal;
                double upDelay;
                double downDelay;
                if (string.IsNullOrEmpty(prop[0]) || !TryParseState(prop[1], out defVal) || !double.TryParse(prop[2], out upDelay) || !double.TryParse(prop[3], out downDelay))
                {
                    exceptionsManager.AddModulePropertyException(property);
                    return;
                }

                this.Name = prop[0];
                this.DefaultState = defVal;
                this.StartupDelay = upDelay;
                this.ShutdownDelay = downDelay;
                this.State = defVal;
            }
        }

        public static bool TryParseState(string stateString, out bool state)
        {
            state = false;
            if (stateString.Equals("set") || stateString.Equals("on"))
                state = true;
            else if (stateString.Equals("reset") || stateString.Equals("off"))
                state = false;
            else
                return false;

            return true;
        }
    }
}
