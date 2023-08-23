using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
        public class ScriptExceptions
        {
            private readonly StringBuilder exceptionsMessages = new StringBuilder();
            private readonly DebugLogs debugLogs;
            public int exceptionsCount = 0;

            /// <summary>
            /// Create a new list of exception messages to be thrown all at once
            /// </summary>
            public ScriptExceptions() { }

            /// <summary>
            /// Create a new list of exceptions messages to be thrown all at once, also print them on a debug LCD
            /// </summary>
            /// <param name="debugLogs">Utility class for debug LCD</param>
            public ScriptExceptions(DebugLogs debugLogs)
            {
                this.debugLogs = debugLogs;
            }

            /// <summary>
            /// Throw all the exceptions previously added, if any exist
            /// </summary>
            public void ThrowExceptions()
            {
                if (exceptionsCount == 0)
                    return;

                debugLogs.Append($"ScriptExceptions: {exceptionsCount} exception/s occured, stopping program execution!");
                throw new Exception(exceptionsMessages.ToString());
            }

            /// <summary>
            /// Add a generic exception message to the exceptions list
            /// </summary>
            /// <param name="message">Text to be added</param>
            public void AddException(string message)
            {
                exceptionsMessages.AppendLine(message);
                debugLogs.Append(message);
                exceptionsCount++;
            }

            /// <summary>
            /// Add the result of a MyIni.TryParse to the exceptions list
            /// </summary>
            /// <param name="result">The result from the TryParse method</param>
            public void AddMyIniParseException(MyIniParseResult result)
            {
                exceptionsMessages.AppendLine($"MyIniParseException: {result}");
                debugLogs.Append($"MyIniParseException: {result}");
                exceptionsCount++;
            }

            /// <summary>
            /// Add a missing ini key exception to the exceptions list
            /// </summary>
            /// <param name="name">Name of the module that is missing the key</param>
            /// <param name="key">Key that is missing</param>
            public void AddMissingIniKeyException(string name, string key)
            {
                exceptionsMessages.AppendLine($"MissinIniKeyException: Module {name} is missing required key '{key}'");
                debugLogs.Append($"MissinIniKeyException: Module {name} is missing required key '{key}'");
                exceptionsCount++;
            }

            /// <summary>
            /// Add a missing ini value exception to the exceptions list
            /// </summary>
            /// <param name="name">Name of the module that is missing the value</param>
            /// <param name="key">The key of witch the value is missing</param>
            public void AddMissingIniValueException(string name, string key)
            {
                exceptionsMessages.AppendLine($"MissingIniValueException: Module {name} is missing the value of key '{key}'");
                debugLogs.Append($"MissingIniValueException: Module {name} is missing the value of key '{key}'");
                exceptionsCount++;
            }

            public void AddModulePropertyException(string property)
            {
                exceptionsMessages.AppendLine($"ModulePropertyException: Could not parse property '{property}'");
                debugLogs.Append($"ModulePropertyException: Could not parse property '{property}'");
                exceptionsCount++;
            }

            /// <summary>
            /// Add the string from ModuleAction.TryParse to the exceptions list
            /// </summary>
            /// <param name="rawAction">The string that failed to be parsed</param>
            public void AddModuleActionParseException(string rawAction)
            {
                exceptionsMessages.AppendLine($"ModuleActionParseException: Failed to parse raw action '{rawAction}'");
                debugLogs.Append($"ModuleActionParseException: Failed to parse raw action '{rawAction}'");
                exceptionsCount++;
            }

            /// <summary>
            /// Add a Module Properties Missmatch exception to the exceptions list
            /// </summary>
            /// <param name="name">The name of the module</param>
            public void AddModulePropertiesMissmatchException(string name)
            {
                exceptionsMessages.AppendLine($"ModulePropertiesMissmatchException: Module {name} has different properties");
                debugLogs.Append($"ModulePropertiesMissmatchException: Module {name} has different properties");
                exceptionsCount++;
            }
        }
    }
}
