using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using VRage.Game.Entity;

namespace SMS
{
    /// <summary>
    /// https://github.com/THDigi/PBDrawAPI/blob/master/Data/Scripts/PBDebugAPI/PBInterface.cs
    /// </summary>
    public class PBInterface
    {
        public readonly string PropertyId;
        public bool Created { get; private set; }

        ImmutableDictionary<string, Delegate> Functions;
        ImmutableDictionary<string, Delegate>.Builder Builder;

        public PBInterface(string propertyId)
        {
            PropertyId = propertyId;
            Builder = ImmutableDictionary.CreateBuilder<string, Delegate>();

            // need to add terminal controls/properties on first spawn of the block type we care about
            MyEntities.OnEntityCreate += EntityCreated;
        }

        public void Dispose()
        {
            MyEntities.OnEntityCreate -= EntityCreated;
        }

        public void AddMethod(string name, Delegate method)
        {
            if (Created)
                throw new Exception("Cannot add methods after API was already finalized.");

            Builder.Add(name, method);
        }

        void EntityCreated(MyEntity ent) // NOTE: called from a thread
        {
            IMyProgrammableBlock pb = ent as IMyProgrammableBlock;
            if (pb != null)
            {
                // only need the first PB
                MyEntities.OnEntityCreate -= EntityCreated;

                SubmitAndCreate();
            }
        }

        void SubmitAndCreate()
        {
            if (Created)
                return;

            Created = true;
            Functions = Builder.ToImmutable();
            Builder = null;

            var p = MyAPIGateway.TerminalControls.CreateProperty<IReadOnlyDictionary<string, Delegate>, IMyProgrammableBlock>(PropertyId);
            p.Getter = Getter;
            p.Setter = (b, v) => { };
            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(p);
        }

        IReadOnlyDictionary<string, Delegate> Getter(IMyTerminalBlock block)
        {
            if (Functions == null)
                throw new Exception("API was not generated yet... a PB needs to exist first.");

            IMyProgrammableBlock pb = block as IMyProgrammableBlock;
            if (pb == null)
                throw new Exception("The API can only be retrieved from a PB");

            return Functions;
        }
    }
}
