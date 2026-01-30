using ORS_ER.components;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Xml.Linq;

namespace ORS_ER
{
    internal class Creator
    {
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
            input.Code = $"dynamic {input.Outputs.First().Value.name} = {input.Outputs.First().Value.value};\n";
            input.CreateRect(mouseWorldX, mouseWorldY);
            return input;
        }

        private static Component createPrint(string Name, string Description, string Category, int mouseWorldX, int mouseWorldY)
        {
            Print input = new Print(Name, Description, Category);
            input.Selected = true;
            input.Code = $"Console.WriteLine({input.Inputs.First().Value.name});\n";
            input.CreateRect(mouseWorldX, mouseWorldY);
            return input;
        }
    }
}
