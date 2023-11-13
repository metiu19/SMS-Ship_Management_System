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
        public interface IShipModule
        {
            /// <summary>
            /// The name of the module
            /// </summary>
            string Name { get; }
            /// <summary>
            /// The state of the module
            /// </summary>
            States State { get; }

            /// <summary>
            /// Init the module
            /// </summary>
            void Init();

            /// <summary>
            /// Check if current state equals expected state, if no force it
            /// </summary>
            int CheckState();
            /// <summary>
            /// Toggle the state of the module
            /// </summary>
            int ToggleState();
            /// <summary>
            /// Set the state of the module
            /// </summary>
            int SetState(bool state);
            /// <summary>
            /// Try to fix the module if in error status
            /// </summary>
            int TryFixError();
            /// <summary>
            /// Quick module toggle
            /// </summary>
            int Standby();

            /// <summary>
            /// Get all the properties of this module
            /// </summary>
            /// <returns>A dictionary with all the properties names and the correspondig states</returns>
            Dictionary<string, bool> GetProperties();
            /// <summary>
            /// Get the state of a property
            /// </summary>
            /// <param name="propertyName">The name of the property</param>
            /// <returns>The state of the property, null if property doesn't exist</returns>
            bool? GetProperty(string propertyName);
            /// <summary>
            /// Toggle the state of a property
            /// </summary>
            /// <param name="propertyName">The name of the property</param>
            int ToggleProperty(string propertyName);
            /// <summary>
            /// Set the state of a property
            /// </summary>
            /// <param name="propertyName">The name of the property</param>
            /// <param name="state">The state to set</param>
            int SetProperty(string propertyName, bool state);
        }
    }
}
