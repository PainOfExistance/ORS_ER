using Microsoft.Win32;
using ORS_ER.components;
using ORS_ER.connections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ORS_ER
{
    internal class Creator
    {
        internal sealed class DiagramData
        {
            [JsonPropertyName("components")]
            public List<ComponentData> Components { get; set; } = [];

            [JsonPropertyName("connections")]
            public List<ConnectionData> Connections { get; set; } = [];
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

            // NEW (multi) format only
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

        public static void Save(Dictionary<string, Component> components, Dictionary<string, Connection> connections)
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
            saveBuilder.Append("{\n\"components\": [\n");
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

        public static (Dictionary<string, Component>, Dictionary<string, Connection>) Load()
        {
            try
            {
                OpenFileDialog openFileDialog = new();
                openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.ShowDialog();
                string filePath = "";

                if (openFileDialog.FileName != "")
                    filePath = openFileDialog.FileName;

                var jsonData = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };

                var rawData = JsonSerializer.Deserialize<DiagramData>(jsonData, options);
                Dictionary<string, Component> components = new Dictionary<string, Component>();
                Dictionary<string, Connection> connections = new Dictionary<string, Connection>();

                foreach (var component in rawData.Components)
                {
                    Component newComponent = Create(component.Name, component.Description, component.Category, (int)component.X, (int)component.Y);
                    newComponent.SetId(component.Id);
                    newComponent.Code = component.Code ?? "";

                    newComponent.Inputs.Clear();
                    foreach (var input in component.Inputs)
                    {
                        IO newIO = new IO();
                        newIO.SetId(input.Id);
                        newIO.inputConnectionIds = input.InputIds ?? [];
                        newIO.outputConnectionIds = input.OutputIds ?? [];
                        newComponent.Inputs.Add(input.Id, newIO);
                    }

                    newComponent.Outputs.Clear();
                    foreach (var output in component.Outputs)
                    {
                        IO newIO = new IO();
                        newIO.SetId(output.Id);
                        newIO.inputConnectionIds = output.InputIds ?? [];
                        newIO.outputConnectionIds = output.OutputIds ?? [];
                        newComponent.Outputs.Add(output.Id, newIO);
                    }

                    newComponent.CreateRect((int)component.X, (int)component.Y);
                    components.Add(newComponent.GetId(), newComponent);
                }

                foreach (var connection in rawData.Connections)
                {
                    Connection newConnection = new Connection(connection.FromId, connection.ToId, connection.FromComponentId, connection.ToComponentId);
                    newConnection.SetId(connection.Id);
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

        public static Component Create(string Name, string Description, string Category, int mouseWorldX, int mouseWorldY)
        {
            switch (Name)
            {
                case "String Input":
                    return createInput(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "Numerical Input":
                    return createInput(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "Print":
                    return createPrint(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "Binary Input":
                    return createInput(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "Logic Block":
                    return createLogic(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "Logic Gate Block":
                    return createLogicGate(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "Operator Block":
                    return createOperator(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "If":
                    return createIf(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "While":
                    return createWhile(Name, Description, Category, mouseWorldX, mouseWorldY);
                default:
                    return createInput(Name, Description, Category, mouseWorldX, mouseWorldY);
            }
        }

        public static Component createInput(string Name, string Description, string Category, int mouseWorldX, int mouseWorldY)
        {
            Input input = new Input(Name, Description, Category);
            input.Selected = true;
            input.CreateRect(mouseWorldX, mouseWorldY);
            return input;
        }

        private static Component createPrint(string Name, string Description, string Category, int mouseWorldX, int mouseWorldY)
        {
            Print input = new Print(Name, Description, Category);
            input.Selected = true;
            runningIndex++;
            input.CreateRect(mouseWorldX, mouseWorldY);
            return input;
        }

        private static Component createLogic(string Name, string Description, string Category, int mouseWorldX, int mouseWorldY)
        {
            Logic input = new Logic(Name, Description, Category);
            input.Selected = true;
            runningIndex++;
            input.CreateRect(mouseWorldX, mouseWorldY);
            return input;
        }

        private static Component createLogicGate(string Name, string Description, string Category, int mouseWorldX, int mouseWorldY)
        {
            Gate input = new Gate(Name, Description, Category);
            input.Selected = true;
            runningIndex++;
            input.CreateRect(mouseWorldX, mouseWorldY);
            return input;
        }

        private static Component createOperator(string Name, string Description, string Category, int mouseWorldX, int mouseWorldY)
        {
            Operator input = new Operator(Name, Description, Category);
            input.Selected = true;
            runningIndex++;
            input.CreateRect(mouseWorldX, mouseWorldY);
            return input;
        }

        private static Component createIf(string Name, string Description, string Category, int mouseWorldX, int mouseWorldY)
        {
            If input = new If(Name, Description, Category);
            input.Selected = true;
            runningIndex++;
            input.CreateRect(mouseWorldX, mouseWorldY);
            return input;
        }

        private static Component createWhile(string Name, string Description, string Category, int mouseWorldX, int mouseWorldY)
        {
            While input = new While(Name, Description, Category);
            input.Selected = true;
            runningIndex++;
            input.CreateRect(mouseWorldX, mouseWorldY);
            return input;
        }
    }
}
