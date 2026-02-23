using Microsoft.Win32;
using ORS_ER.components;
using ORS_ER.connections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ORS_ER
{
    internal class Creator
    {
        internal sealed class DiagramData
        {
            [JsonPropertyName("diagramType")]
            public string? DiagramType { get; set; }

            [JsonPropertyName("components")]
            public List<ComponentData> Components { get; set; } = [];

            [JsonPropertyName("connections")]
            public List<ConnectionData> Connections { get; set; } = [];
        }

        internal sealed class SubCircuitData
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("category")]
            public string? Category { get; set; }

            [JsonPropertyName("inputs")]
            public List<SubCircuitPinData> Inputs { get; set; } = [];

            [JsonPropertyName("outputs")]
            public List<SubCircuitPinData> Outputs { get; set; } = [];

            [JsonPropertyName("diagram")]
            public DiagramData? Diagram { get; set; }
        }

        internal sealed class SubCircuitPinData
        {
            [JsonPropertyName("componentId")]
            public string? ComponentId { get; set; }

            [JsonPropertyName("ioId")]
            public string? IoId { get; set; }
        }

        internal sealed class ComponentData
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("x")]
            public float X { get; set; }

            [JsonPropertyName("y")]
            public float Y { get; set; }

            [JsonPropertyName("code")]
            public string? Code { get; set; }

            [JsonPropertyName("value")]
            public JsonElement? Value { get; set; }

            [JsonPropertyName("isInsideIf")]
            public string? IsInsideIf { get; set; }

            [JsonPropertyName("isInsideWhile")]
            public string? IsInsideWhile { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("category")]
            public string? Category { get; set; }

            [JsonPropertyName("inputs")]
            public List<IoData> Inputs { get; set; } = [];

            [JsonPropertyName("outputs")]
            public List<IoData> Outputs { get; set; } = [];

            [JsonPropertyName("index")]
            public int? Index { get; set; }

            [JsonPropertyName("operation")]
            public string? Operation { get; set; }
        }

        internal sealed class IoData
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("value")]
            public string? Value { get; set; }

            [JsonPropertyName("ifTrue")]
            public string? IfTrue { get; set; }

            [JsonPropertyName("inputIds")]
            public List<string>? InputIds { get; set; }

            [JsonPropertyName("outputIds")]
            public List<string>? OutputIds { get; set; }
        }

        internal sealed class ConnectionData
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("fromId")]
            public string? FromId { get; set; }

            [JsonPropertyName("toId")]
            public string? ToId { get; set; }

            [JsonPropertyName("fromComponentId")]
            public string? FromComponentId { get; set; }

            [JsonPropertyName("toComponentId")]
            public string? ToComponentId { get; set; }
        }

        private static readonly Dictionary<string, SubCircuitData> CachedLogicComponents = new(StringComparer.OrdinalIgnoreCase);

        public static void Save(Dictionary<string, Component> components, Dictionary<string, Connection> connections, string diagramType)
        {
            static void TrimTrailing(StringBuilder builder)
            {
                while (builder.Length > 0)
                {
                    var ch = builder[^1];
                    if (ch != ',' && ch != '\n')
                        break;

                    builder.Length--;
                }
            }

            var saveBuilder = new StringBuilder();
            saveBuilder.Append("{\n\"diagramType\": \"")
                .Append(diagramType)
                .Append("\",\n\"components\": [\n");
            foreach (var component in components.Values)
            {
                saveBuilder.Append(component.ToJson()).Append(",\n");
            }
            TrimTrailing(saveBuilder);
            saveBuilder.Append("\n],\n\"connections\": [\n");

            foreach (var connection in connections.Values)
            {
                saveBuilder.Append(connection.ToJson()).Append(",\n");
            }
            TrimTrailing(saveBuilder);
            saveBuilder.Append("\n]\n}");
            string saveData = saveBuilder.ToString();

            try
            {
                SaveFileDialog SD = new SaveFileDialog();
                SD.Filter = "Json (*.json)|*.json|Show All Files (*.*)|*.*";
                SD.FileName = "diagram";
                SD.Title = "Save As";
                SD.ShowDialog();
                if (SD.FileName != "")
                {
                    SD.FileName = SD.FileName.EndsWith(".json") ? SD.FileName : SD.FileName + ".json";
                    File.WriteAllText(SD.FileName, saveData);
                }
                Debug.WriteLine("Saved file " + SD.FileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving file: " + ex.Message);
            }
        }

        public static (Dictionary<string, Component>, Dictionary<string, Connection>) Load(string expectedDiagramType)
        {
            try
            {
                OpenFileDialog openFileDialog = new();
                openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.ShowDialog();
                if (string.IsNullOrWhiteSpace(openFileDialog.FileName))
                    return (new Dictionary<string, Component>(), new Dictionary<string, Connection>());

                string filePath = openFileDialog.FileName;

                var jsonData = File.ReadAllText(filePath);
                var options = GetDiagramJsonOptions();

                var rawData = JsonSerializer.Deserialize<DiagramData>(jsonData, options);
                if (rawData is null)
                    return (new Dictionary<string, Component>(), new Dictionary<string, Connection>());

                if (!string.Equals(rawData.DiagramType, expectedDiagramType, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine("Diagram type mismatch when loading file.");
                    return (new Dictionary<string, Component>(), new Dictionary<string, Connection>());
                }
                Dictionary<string, Component> components = new Dictionary<string, Component>();
                Dictionary<string, Connection> connections = new Dictionary<string, Connection>();

                foreach (var component in rawData.Components)
                {
                    Component newComponent = Create(component.Name, component.Description, component.Category, (int)component.X, (int)component.Y);
                    newComponent.SetId(component.Id);
                    newComponent.Code = component.Code ?? "";
                    newComponent.IsInsideIf = component.IsInsideIf ?? "";
                    newComponent.IsInsideWhile = component.IsInsideWhile ?? "";

                    if (component.Value is JsonElement ve)
                    {
                        try
                        {
                            if (ve.ValueKind == JsonValueKind.Object)
                            {
                                var name = ve.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : "";
                                dynamic d1 = null;
                                if (ve.TryGetProperty("value", out var v))
                                {
                                    d1 = v.ValueKind switch
                                    {
                                        JsonValueKind.String => v.GetString() ?? "",
                                        JsonValueKind.Number => v.TryGetDouble(out var d) ? d : v,
                                        JsonValueKind.True => true,
                                        JsonValueKind.False => false,
                                        JsonValueKind.Null => null,
                                        _ => v
                                    };
                                }
                                newComponent.Value = (name ?? "", d1);
                            }
                            else if (ve.ValueKind == JsonValueKind.Array && ve.GetArrayLength() == 2)
                            {
                                var item0 = ve[0];
                                var item1 = ve[1];
                                var s0 = item0.ValueKind == JsonValueKind.String ? item0.GetString() : item0.ToString();
                                dynamic d1 = item1.ValueKind == JsonValueKind.String ? (item1.GetString() ?? "") : item1;
                                newComponent.Value = (s0 ?? "", d1);
                            }
                        }
                        catch
                        {
                        }
                    }

                    newComponent.Inputs.Clear();
                    foreach (var input in component.Inputs)
                    {
                        IO newIO = new IO();
                        newIO.SetId(input.Id);
                        newIO.IfTrue = input.IfTrue ?? "";
                        if (input.InputIds != null || input.OutputIds != null)
                        {
                            newIO.InputConnectionIds = input.InputIds ?? [];
                            newIO.OutputConnectionIds = input.OutputIds ?? [];
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(input.Value))
                                newIO.InputConnectionIds = [input.Value];
                        }

                        var ioKey = string.IsNullOrWhiteSpace(input.Id) ? newIO.GetId() : input.Id;
                        newComponent.Inputs.Add(ioKey, newIO);
                    }

                    newComponent.Outputs.Clear();
                    foreach (var output in component.Outputs)
                    {
                        IO newIO = new IO();
                        newIO.SetId(output.Id);
                        newIO.IfTrue = output.IfTrue ?? "";
                        if (output.InputIds != null || output.OutputIds != null)
                        {
                            newIO.InputConnectionIds = output.InputIds ?? [];
                            newIO.OutputConnectionIds = output.OutputIds ?? [];
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(output.Value))
                                newIO.OutputConnectionIds = [output.Value];
                        }

                        var ioKey = string.IsNullOrWhiteSpace(output.Id) ? newIO.GetId() : output.Id;
                        newComponent.Outputs.Add(ioKey, newIO);
                    }

                    newComponent.CreateRect((int)component.X, (int)component.Y);
                    components.Add(newComponent.GetId(), newComponent);
                }

                foreach (var conn in rawData.Connections)
                {
                    Connection newConnection = new Connection(conn.FromId, conn.ToId, conn.FromComponentId, conn.ToComponentId);
                    newConnection.SetId(conn.Id);
                    connections.Add(newConnection.GetId(), newConnection);
                }

                return (components, connections);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading file: " + ex.Message);
                return (new Dictionary<string, Component>(), new Dictionary<string, Connection>());
            }
        }

        internal static (Dictionary<string, Component>, Dictionary<string, Connection>) BuildLogicCircuit(DiagramData rawData)
        {
            var components = new Dictionary<string, Component>();
            var connections = new Dictionary<string, Connection>();

            foreach (var component in rawData.Components)
            {
                var newComponent = CreateLG(component.Name ?? "", component.Description ?? "", component.Category ?? "", (int)component.X, (int)component.Y);
                newComponent.SetId(component.Id ?? newComponent.GetId());
                newComponent.Code = component.Code ?? "";
                newComponent.IsInsideIf = component.IsInsideIf ?? "";
                newComponent.IsInsideWhile = component.IsInsideWhile ?? "";

                if (component.Value is JsonElement ve)
                    SetComponentValue(newComponent, ve);

                newComponent.Inputs.Clear();
                foreach (var input in component.Inputs)
                {
                    var newIO = new IO();
                    newIO.SetId(input.Id ?? newIO.GetId());
                    newIO.IfTrue = input.IfTrue ?? "";
                    if (input.InputIds != null || input.OutputIds != null)
                    {
                        newIO.InputConnectionIds = input.InputIds ?? [];
                        newIO.OutputConnectionIds = input.OutputIds ?? [];
                    }
                    else if (!string.IsNullOrWhiteSpace(input.Value))
                    {
                        newIO.InputConnectionIds = [input.Value];
                    }

                    newComponent.Inputs.Add(newIO.GetId(), newIO);
                }

                newComponent.Outputs.Clear();
                foreach (var output in component.Outputs)
                {
                    var newIO = new IO();
                    newIO.SetId(output.Id ?? newIO.GetId());
                    newIO.IfTrue = output.IfTrue ?? "";
                    if (output.InputIds != null || output.OutputIds != null)
                    {
                        newIO.InputConnectionIds = output.InputIds ?? [];
                        newIO.OutputConnectionIds = output.OutputIds ?? [];
                    }
                    else if (!string.IsNullOrWhiteSpace(output.Value))
                    {
                        newIO.OutputConnectionIds = [output.Value];
                    }

                    newComponent.Outputs.Add(newIO.GetId(), newIO);
                }

                newComponent.CreateRect((int)component.X, (int)component.Y);
                components.Add(newComponent.GetId(), newComponent);
            }

            foreach (var connection in rawData.Connections)
            {
                var newConnection = new Connection(connection.FromId ?? "", connection.ToId ?? "", connection.FromComponentId ?? "", connection.ToComponentId ?? "");
                if (!string.IsNullOrWhiteSpace(connection.Id))
                    newConnection.SetId(connection.Id);
                connections.Add(newConnection.GetId(), newConnection);
            }

            return (components, connections);
        }

        public static SubCircuitData? SaveLogicComponent(Dictionary<string, Component> components, Dictionary<string, Connection> connections, string? name = null, string? description = null, string? category = null)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Json (*.json)|*.json|Show All Files (*.*)|*.*",
                    FileName = string.IsNullOrWhiteSpace(name) ? "logic_component" : name,
                    Title = "Save Logic Component"
                };

                dialog.ShowDialog();
                if (string.IsNullOrWhiteSpace(dialog.FileName))
                    return null;

                var filePath = dialog.FileName.EndsWith(".json") ? dialog.FileName : dialog.FileName + ".json";
                var componentName = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(filePath) : name;
                var componentDescription = string.IsNullOrWhiteSpace(description) ? "Custom logic component." : description;
                var componentCategory = string.IsNullOrWhiteSpace(category) ? "Custom" : category;

                var orderedInputs = components.Values
                    .OfType<BinaryInput>()
                    .OrderBy(component => component.Rect.MidX)
                    .ThenBy(component => component.Rect.MidY)
                    .Select(component => new SubCircuitPinData
                    {
                        ComponentId = component.GetId(),
                        IoId = component.Outputs.Keys.FirstOrDefault()
                    })
                    .ToList();

                var orderedOutputs = components.Values
                    .OfType<BinaryOutput>()
                    .OrderBy(component => component.Rect.MidX)
                    .ThenBy(component => component.Rect.MidY)
                    .Select(component => new SubCircuitPinData
                    {
                        ComponentId = component.GetId(),
                        IoId = component.Inputs.Keys.FirstOrDefault()
                    })
                    .ToList();

                var excludedIds = new HashSet<string>(orderedInputs.Select(p => p.ComponentId ?? string.Empty).Where(id => id.Length > 0));
                excludedIds.UnionWith(orderedOutputs.Select(p => p.ComponentId ?? string.Empty).Where(id => id.Length > 0));

                var diagram = BuildDiagramData(components, connections, excludedIds);
                var data = new SubCircuitData
                {
                    Name = componentName,
                    Description = componentDescription,
                    Category = componentCategory,
                    Inputs = orderedInputs,
                    Outputs = orderedOutputs,
                    Diagram = diagram
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
                CacheLogicComponent(data);
                Debug.WriteLine("Saved logic component " + filePath);
                return data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving logic component: " + ex.Message);
                return null;
            }
        }

        public static SubCircuitData? LoadLogicComponentFromFile()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    FilterIndex = 2,
                    RestoreDirectory = true
                };

                dialog.ShowDialog();
                if (string.IsNullOrWhiteSpace(dialog.FileName))
                    return null;

                var jsonData = File.ReadAllText(dialog.FileName);
                var options = GetDiagramJsonOptions();
                var data = JsonSerializer.Deserialize<SubCircuitData>(jsonData, options);
                if (data is null || string.IsNullOrWhiteSpace(data.Name))
                    return null;

                CacheLogicComponent(data);
                return data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading logic component: " + ex.Message);
                return null;
            }
        }

        public static IReadOnlyCollection<SubCircuitData> GetCachedLogicComponents() => CachedLogicComponents.Values.ToList();

        public static bool TryGetLogicComponent(string name, out SubCircuitData data) => CachedLogicComponents.TryGetValue(name, out data);

        private static void CacheLogicComponent(SubCircuitData data)
        {
            if (string.IsNullOrWhiteSpace(data.Name))
                return;

            CachedLogicComponents[data.Name] = data;
        }

        private static DiagramData BuildDiagramData(Dictionary<string, Component> components, Dictionary<string, Connection> connections, HashSet<string> excludedIds)
        {
            var diagram = new DiagramData();
            var options = GetDiagramJsonOptions();

            foreach (var component in components.Values)
            {
                if (excludedIds.Contains(component.GetId()))
                    continue;

                var json = component.ToJson();
                var data = JsonSerializer.Deserialize<ComponentData>(json, options);
                if (data is not null)
                    diagram.Components.Add(data);
            }

            foreach (var connection in connections.Values)
            {
                diagram.Connections.Add(new ConnectionData
                {
                    Id = connection.GetId(),
                    FromId = connection.FromId,
                    ToId = connection.ToId,
                    FromComponentId = connection.FromComponentId,
                    ToComponentId = connection.ToComponentId
                });
            }

            return diagram;
        }

        private static JsonSerializerOptions GetDiagramJsonOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
        }

        private static void SetComponentValue(Component newComponent, JsonElement ve)
        {
            try
            {
                if (ve.ValueKind == JsonValueKind.Object)
                {
                    var name = ve.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : "";
                    dynamic d1 = null;
                    if (ve.TryGetProperty("value", out var v))
                    {
                        d1 = v.ValueKind switch
                        {
                            JsonValueKind.String => v.GetString() ?? "",
                            JsonValueKind.Number => v.TryGetDouble(out var d) ? d : v,
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Null => null,
                            _ => v
                        };
                    }
                    newComponent.Value = (name ?? "", d1);
                }
                else if (ve.ValueKind == JsonValueKind.Array && ve.GetArrayLength() == 2)
                {
                    var item0 = ve[0];
                    var item1 = ve[1];
                    var s0 = item0.ValueKind == JsonValueKind.String ? item0.GetString() : item0.ToString();
                    dynamic d1 = item1.ValueKind == JsonValueKind.String ? (item1.GetString() ?? "") : item1;
                    newComponent.Value = (s0 ?? "", d1);
                }
            }
            catch
            {
            }
        }

        public static Component Create(string Name, string Description, string Category, int mouseWorldX, int mouseWorldY)
        {
            switch (Name)
            {
                case "String Input":
                    return CreateInput(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "Numerical Input":
                    return CreateInput(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "Print":
                    return CreatePrint(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "Binary Input":
                    return CreateInput(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "Operator Block":
                    return CreateOperator(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "If":
                    return CreateIf(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "While":
                    return CreateWhile(Name, Description, Category, mouseWorldX, mouseWorldY);
                default:
                    return CreateInput(Name, Description, Category, mouseWorldX, mouseWorldY);
            }
        }

        public static Component CreateInput(string name, string description, string category, int mouseWorldX, int mouseWorldY)
        {
            var input = new Input(name, description, category);
            input.Selected = true;
            input.CreateRect(mouseWorldX, mouseWorldY);
            return input;
        }

        private static Component CreatePrint(string name, string description, string category, int mouseWorldX, int mouseWorldY)
        {
            var printComponent = new Print(name, description, category);
            printComponent.Selected = true;
            printComponent.CreateRect(mouseWorldX, mouseWorldY);
            return printComponent;
        }

        private static Component CreateOperator(string name, string description, string category, int mouseWorldX, int mouseWorldY)
        {
            var operatorComponent = new Operator(name, description, category);
            operatorComponent.Selected = true;
            operatorComponent.CreateRect(mouseWorldX, mouseWorldY);
            return operatorComponent;
        }

        private static Component CreateIf(string name, string description, string category, int mouseWorldX, int mouseWorldY)
        {
            var ifComponent = new If(name, description, category);
            ifComponent.Selected = true;
            ifComponent.CreateRect(mouseWorldX, mouseWorldY);
            return ifComponent;
        }

        private static Component CreateWhile(string name, string description, string category, int mouseWorldX, int mouseWorldY)
        {
            var whileComponent = new While(name, description, category);
            whileComponent.Selected = true;
            whileComponent.CreateRect(mouseWorldX, mouseWorldY);
            return whileComponent;
        }


        public static Component CreateLG(string Name, string Description, string Category, int mouseWorldX, int mouseWorldY)
        {
            if (TryGetLogicComponent(Name, out var cached))
            {
                var customComponent = new SubCircuitComponent(cached);
                customComponent.Selected = true;
                customComponent.CreateRect(mouseWorldX, mouseWorldY);
                return customComponent;
            }

            switch (Name)
            {
                case "Binary Input":
                    var binaryInput = new BinaryInput(Name, Description, Category);
                    binaryInput.Selected = true;
                    binaryInput.CreateRect(mouseWorldX, mouseWorldY);
                    return binaryInput;
                case "Binary Output":
                    var binaryOutput = new BinaryOutput(Name, Description, Category);
                    binaryOutput.Selected = true;
                    binaryOutput.CreateRect(mouseWorldX, mouseWorldY);
                    return binaryOutput;
                case "Half Adder":
                    var halfAdder = new Adder(Name, Description, Category);
                    halfAdder.Selected = true;
                    halfAdder.CreateRect(mouseWorldX, mouseWorldY);
                    return halfAdder;
                case "Full Adder":
                    var fullAdder = new Adder(Name, Description, Category);
                    fullAdder.Selected = true;
                    fullAdder.CreateRect(mouseWorldX, mouseWorldY);
                    return fullAdder;
                default:
                    var gateComponent = new Gate(Name, Description, Category);
                    gateComponent.Selected = true;
                    gateComponent.CreateRect(mouseWorldX, mouseWorldY);
                    return gateComponent;
            }
        }
    }
}
