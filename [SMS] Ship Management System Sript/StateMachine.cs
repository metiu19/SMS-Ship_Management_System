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
        public class StateMachine
        {
            private readonly Program program;
            private readonly List<IEnumerator<bool>> SerialQueue;
            private readonly List<IEnumerator<bool>> ParallelQueue;

            public StateMachine(Program program)
            {
                this.program = program;
                this.SerialQueue = new List<IEnumerator<bool>>();
                this.ParallelQueue = new List<IEnumerator<bool>>();
            }

            public void AddToSerialQueue(IEnumerator<bool> function)
            {
                if (function == null)
                    return;

                SerialQueue.Add(function);
                program.Runtime.UpdateFrequency |= UpdateFrequency.Once;
            }

            public void AddToParallelQueue(IEnumerator<bool> function)
            {
                if (function == null)
                    return;

                ParallelQueue.Add(function);
                program.Runtime.UpdateFrequency |= UpdateFrequency.Once;
            }

            public void Tick()
            {
                RunSerial();
                RunParallel();
            }

            private void RunSerial()
            {
                bool hasMoreSteps = SerialQueue[0].MoveNext();

                if (hasMoreSteps)
                {
                    program.Runtime.UpdateFrequency |= UpdateFrequency.Once;
                }
                else
                {
                    SerialQueue[0].Dispose();
                    SerialQueue.RemoveAt(0);

                    if (SerialQueue.Count > 0)
                        program.Runtime.UpdateFrequency |= UpdateFrequency.Once;
                }
            }

            private void RunParallel()
            {
                foreach (IEnumerator<bool> stateMachine in ParallelQueue)
                {
                    bool hasMoreSteps = stateMachine.MoveNext();

                    if (hasMoreSteps)
                        program.Runtime.UpdateFrequency |= UpdateFrequency.Once;
                    else
                    {
                        stateMachine.Dispose();
                        ParallelQueue.Remove(stateMachine);
                    }
                }
            }
        }
    }
}
