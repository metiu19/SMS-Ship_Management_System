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
        public class DebugLogs
        {
            private readonly IMyTextPanel panel;
            private readonly string scriptName;
            private readonly string scriptVersion;
            public bool Enable = true;

            /// <summary>
            /// Utility class for interactions with a Debug LCD
            /// </summary>
            /// <param name="panel">The Debug LCD where to write</param>
            /// <param name="scriptName">The name of the script</param>
            /// <param name="scriptVersion">The version of the script</param>
            public DebugLogs(IMyTextPanel panel, string scriptName, string scriptVersion)
            {
                this.panel = panel;
                this.scriptName = scriptName;
                this.scriptVersion = scriptVersion;

                if (panel != null)
                {
                    this.panel.ContentType = ContentType.TEXT_AND_IMAGE;
                    this.panel.FontSize = 0.7f;
                    Clear();
                }
            }

            /// <summary>
            /// Append the message and a new line to the panel
            /// </summary>
            /// <param name="message">String to be appended</param>
            public void Append(string message)
            {
                if (Enable)
                    panel?.WriteText($"{message}\n", true);
            }

            /// <summary>
            /// Clear screen content and print the message with a new line
            /// </summary>
            /// <param name="message">String to be printed</param>
            public void Write(string message)
            {
                if (!Enable)
                    return;
                Clear();
                Append(message);
            }

            /// <summary>
            /// Cleare all the text from the panel
            /// </summary>
            public void Clear()
            {
                if (Enable)
                    panel?.WriteText($"Debug Screen - {scriptName} - [{scriptVersion}]\n\n", false);
            }
        }
    }
}
