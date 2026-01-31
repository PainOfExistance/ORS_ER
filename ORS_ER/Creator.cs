using Microsoft.Win32;
using ORS_ER.components;
using ORS_ER.connections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Xml.Linq;

namespace ORS_ER
{
    internal class Creator
    {
        private static int runningIndex = 0;

        public static int Save(Dictionary<string, Component> components, Dictionary<string, Connection> connections)
        {
            string saveData = "{\n\"components\": [\n";
            foreach (var component in components.Values)
            {
                saveData += component.ToJson() + ",\n";
            }
            foreach (var connection in connections.Values)
            {
                saveData += connection.ToJson() + ",\n";
            }
            saveData = saveData.TrimEnd(',', '\n') + "\n]\n}";

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
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving file: " + ex.Message);
                return -1;
            }
        }

        public static Component Create(string Name, string Description, string Category, int mouseWorldX, int mouseWorldY)
        {
            switch (Name)
            {
                case "Input":
                    return createInput(Name, Description, Category, mouseWorldX, mouseWorldY);
                case "Print":
                    return createPrint(Name, Description, Category, mouseWorldX, mouseWorldY);
                /*case "Process":
                    return createProcess(type, Name, Description, Category);*/
                default:
                    throw new ArgumentException("Invalid component type");
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
            input.Index = runningIndex;
            input.CreateRect(mouseWorldX, mouseWorldY);
            return input;
        }
    }
}
