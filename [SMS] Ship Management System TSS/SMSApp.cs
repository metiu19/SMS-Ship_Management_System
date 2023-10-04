using Sandbox.ModAPI;
using SMS.TouchAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game.ModAPI;
using VRageMath;

namespace SMS
{
    public class SMSApp : TouchApp
    {
        public IMyProgrammableBlock PB;
        public List<Module> Modules { get; private set; }
        public readonly string ID;

        readonly string version = "v0.0.1";
        bool init = false;
        View window;
        Switch modulesSwitch;

        public SMSApp(IMyCubeBlock block, IMyTextSurface surface) : base(block, surface)
        {
            ID = block.EntityId.ToString();
            Modules = new List<Module>();

            DefaultBg = true;

            var windowBar = new WindowBar($"[SMS] Ship Management System - [{version}]");
            windowBar.BgColor = Color.DarkCyan;
            windowBar.Label.TextColor = Color.White;

            window = new View();
            window.Border = new Vector4(3);
            window.Padding = new Vector4(10);

            AddChild(windowBar);
            AddChild(window);
        }

        public void RegisterModule(string moduleName, int moduleState, Dictionary<string, bool> properties)
        {
            if (!init)
                Modules.Add(new Module(moduleName, moduleState, properties, this));
        }

        public bool LoadModules()
        {
            if (Modules.Count == 0)
                return false;

            if (init)
                return false;
            init = true;

            string[] modulesNames = new string[Modules.Count];
            for (int x = 0; x < Modules.Count; x++)
                modulesNames[x] = Modules[x].Name;
            modulesSwitch = new Switch(modulesNames, 0, (index) =>
            {
                Modules.ForEach(module => module.Enabled = false);
                Modules[index].Enabled = true;
            });
            window.AddChild(modulesSwitch);

            Modules[0].Enabled = true;
            foreach (var module in Modules)
            {
                window.AddChild(module);
            }
            return true;
        }

        public void ResetApp()
        {
            PB = null;
            Modules = new List<Module>();
            init = false;
            window.Children.ForEach(c => window.RemoveChild(c));
        }
    }

    public enum States
    {
        Error = -1,
        Disabled = 0,
        Enabled = 1,
        GoingDown = 2,
        ComingUp = 3,
    }

    public class Module : View
    {
        public readonly string Name;
        readonly SMSApp app;
        readonly Dictionary<States, string> StatesNames = new Dictionary<States, string>()
        {
            { States.Error, "Error" },
            { States.Disabled, "Disabled" },
            { States.Enabled, "Enabled" },
            { States.GoingDown, "Shutting Down" },
            { States.ComingUp, "Starting Up" },
        };

        States state;
        Button moduleState;
        Label commandOutput;
        Dictionary<Label, Button> properties;

        public Module(string name, int state, Dictionary<string, bool> properties, SMSApp app) : base(ViewDirection.Column)
        {
            this.Name = name;
            this.app = app;
            this.state = (States)state;

            Border = new Vector4(3);
            Padding = new Vector4(5);
            Alignment = ViewAlignment.Center;
            Anchor = ViewAnchor.Start;
            Gap = 10;

            var stateField = new View(ViewDirection.Row);
            stateField.Flex = new Vector2(1, 0.1f);
            var stateLabel = new Label("Module State");
            this.moduleState = new Button(StatesNames[this.state], () =>
            {
                if (this.state == States.Error)
                    app.PB.Run($"module fix {Name} {app.ID}");
                else
                    app.PB.Run($"module toggle {Name} {app.ID}");
            });
            stateField.AddChild(stateLabel);
            stateField.AddChild(moduleState);
            AddChild(stateField);

            commandOutput = new Label("");
            commandOutput.FontSize = 0.8f;
            AddChild(commandOutput);

            AddChild(new Label("Properties:"));
            this.properties = new Dictionary<Label, Button>();
            foreach (var property in properties)
            {
                this.properties.Add(new Label(property.Key), new Button(property.Value ? "Enabled" : "Disabled", () =>
                {
                    app.PB.Run($"property toggle {Name} {property.Key} {app.ID}");
                }));
            }
            foreach (var property in this.properties)
            {
                var propertyContainer = new View(ViewDirection.Row);
                propertyContainer.Anchor = ViewAnchor.SpaceBetween;
                propertyContainer.Flex = new Vector2(1, 0.1f);
                propertyContainer.AddChild(property.Key);
                propertyContainer.AddChild(property.Value);
                AddChild(propertyContainer);
            }
            Enabled = false;
        }

        public void SetModuleState(int state)
        {
            this.state = (States)state;
            moduleState.Label.Text = StatesNames[this.state];
        }

        public void SetPropertyState(string propertyName, bool state)
        {
            foreach (var property in properties)
            {
                if (property.Key.Text != propertyName)
                    continue;
                property.Value.Label.Text = state ? "Enabled" : "Disabled";
            }
        }

        public void UpdateCommandOutputLabel(string commandOutput, bool error)
        {
            this.commandOutput.Text = commandOutput;
            if (error)
                this.commandOutput.TextColor = Color.Red;
            else
                this.commandOutput.TextColor = App.Theme.WhiteColor;
        }
    }
}
