using ORS_ER.connections;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ORS_ER.components
{
    class SubCircuitComponent : Component
    {
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Operator);
        private readonly Creator.SubCircuitData _data;
        private readonly List<Creator.SubCircuitPinData> _inputPins;
        private readonly List<Creator.SubCircuitPinData> _outputPins;
        private Dictionary<string, Component>? _internalComponents;
        private Dictionary<string, Connection>? _internalConnections;

        public SubCircuitComponent(Creator.SubCircuitData data) : base(data.Name ?? "Custom Component", data.Description ?? "", data.Category ?? "Custom")
        {
            _data = data;
            _inputPins = data.Inputs ?? [];
            _outputPins = data.Outputs ?? [];
            Code = Name;
            InitializeIo();
            EnsureInternalGraph();
        }

        private void InitializeIo()
        {
            Inputs.Clear();
            Outputs.Clear();

            foreach (var _ in _inputPins)
            {
                var input = new IO();
                Inputs.Add(input.GetId(), input);
            }

            for (var i = 0; i < _outputPins.Count; i++)
            {
                var output = new IO();
                if (_outputPins.Count > 1)
                    output.IfTrue = i.ToString();
                Outputs.Add(output.GetId(), output);
            }

            if (_outputPins.Count <= 1)
                Value = ("bool", false);
            if (_outputPins.Count > 1)
                Value = ("bool", new bool[_outputPins.Count]);
        }

        public override void Paint(SKCanvas canvas)
        {
            canvas.DrawRect(Rect, Paints.ComponentFill);
            Font.Size = 16;

            if (Selected)
                canvas.DrawRect(Rect, Paints.SelectedStroke);
            if (!Selected)
                canvas.DrawRect(Rect, Paints.ComponentStroke);

            foreach (var input in Inputs)
                canvas.DrawCircle(input.Value.Node, 8, Paints.IOPaint);

            var outputValues = GetOutputValues();
            var index = 0;
            foreach (var output in Outputs)
            {
                var active = index < outputValues.Length && outputValues[index];
                canvas.DrawCircle(output.Value.Node, 8, active ? Paints.IOPaintActive : Paints.IOPaint);
                index++;
            }

            canvas.DrawRoundRect(InteractionRect, 6, 6, Paints.ButtonFill);
            canvas.DrawRoundRect(InteractionRect, 6, 6, Paints.ButtonStroke);

            float textX = Rect.MidX - (Font.MeasureText(this.Name) / 2);
            float textY = Rect.MidY + Font.Size / 3;
            canvas.DrawText(this.Name, textX, textY, Font, Paints.ButtonTextPaint);
        }

        public override void CreateRect(int x, int y)
        {
            var width = Font.MeasureText(this.Name) + 40;
            var height = 90f;

            Rect = new SKRect(x - width / 2, y - height / 2, x + width / 2, y + height / 2);
            InteractionRect = new SKRect(
                Rect.Left + 10,
                Rect.Top + 10,
                Rect.Right - 10,
                Rect.Bottom - 10);

            var delta = Rect.Width / (Outputs.Count + 1);
            var keys = Outputs.Keys.ToArray();
            for (var i = 0; i < Outputs.Count; i++)
                Outputs[keys[i]].Node = new SKPoint(Rect.Left + delta * (i + 1), Rect.Bottom);

            delta = Rect.Width / (Inputs.Count + 1);
            keys = Inputs.Keys.ToArray();
            for (var i = 0; i < Inputs.Count; i++)
                Inputs[keys[i]].Node = new SKPoint(Rect.Left + delta * (i + 1), Rect.Top);
        }

        public override void RunInternalSimulation(List<bool> vals)
        {
            if (_internalComponents is null || _internalConnections is null)
                return;

            // Feed incoming values into the internal input pins.
            for (var i = 0; i < _inputPins.Count; i++)
            {
                if (!_internalComponents.TryGetValue(_inputPins[i].ComponentId ?? string.Empty, out var component))
                    continue;

                var inputValue = i < vals.Count ? vals[i] : false;
                component.Value = ("bool", inputValue);
            }

            Parser.RunCircuitSimulation(_internalComponents, _internalConnections);

            // Read internal output pins back into this component's value.
            if (_outputPins.Count == 1)
            {
                Value = ("bool", ReadOutputValue(_outputPins[0]));
            }
            if (_outputPins.Count > 1)
            {
                var results = new bool[_outputPins.Count];
                for (var i = 0; i < _outputPins.Count; i++)
                    results[i] = ReadOutputValue(_outputPins[i]);
                Value = ("bool", results);
            }
        }

        private void EnsureInternalGraph()
        {
            if (_internalComponents is not null && _internalConnections is not null)
                return;

            // Build the embedded circuit once per component instance.
            if (_data.Diagram is null)
            {
                _internalComponents = new Dictionary<string, Component>();
                _internalConnections = new Dictionary<string, Connection>();
                return;
            }

            var (components, connections) = Creator.BuildLogicCircuit(_data.Diagram);
            AddPinComponents(components, connections);
            _internalComponents = components;
            _internalConnections = connections;
        }

        private void AddPinComponents(Dictionary<string, Component> components, Dictionary<string, Connection> connections)
        {
            // Create proxy components for exposed input/output pins in the subcircuit.
            foreach (var pin in _inputPins)
            {
                if (string.IsNullOrWhiteSpace(pin.ComponentId) || string.IsNullOrWhiteSpace(pin.IoId))
                    continue;

                if (components.ContainsKey(pin.ComponentId))
                    continue;

                var component = new BinaryInput("Binary Input", "Binary input.", "Inputs");
                component.SetId(pin.ComponentId);
                component.Outputs.Clear();
                var io = new IO();
                io.SetId(pin.IoId);
                component.Outputs.Add(io.GetId(), io);
                component.CreateRect(0, 0);
                components.Add(component.GetId(), component);
            }

            foreach (var pin in _outputPins)
            {
                if (string.IsNullOrWhiteSpace(pin.ComponentId) || string.IsNullOrWhiteSpace(pin.IoId))
                    continue;

                if (components.ContainsKey(pin.ComponentId))
                    continue;

                var component = new BinaryOutput("Binary Output", "Outputs value of the circuit.", "Outputs");
                component.SetId(pin.ComponentId);
                component.Inputs.Clear();
                var io = new IO();
                io.SetId(pin.IoId);
                io.InputConnectionIds = connections.Values
                    .Where(conn => conn.ToComponentId == pin.ComponentId && conn.ToIOId == pin.IoId)
                    .Select(conn => conn.GetId())
                    .ToList();
                component.Inputs.Add(io.GetId(), io);
                component.CreateRect(0, 0);
                components.Add(component.GetId(), component);
            }
        }

        private bool ReadOutputValue(Creator.SubCircuitPinData pin)
        {
            if (string.IsNullOrWhiteSpace(pin.ComponentId))
                return false;

            if (_internalComponents is null || !_internalComponents.TryGetValue(pin.ComponentId, out var component))
                return false;

            return component.Value.Item2 is bool b && b;
        }

        private bool[] GetOutputValues()
        {
            // Normalize the stored value into a boolean array for rendering.
            if (Value.Item2 is bool b)
                return [b];

            if (Value.Item2 is bool[] values)
                return values;

            return Array.Empty<bool>();
        }
    }
}
