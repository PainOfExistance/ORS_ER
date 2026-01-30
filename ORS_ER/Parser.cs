using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ORS_ER.components;
using ORS_ER.connections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System .Reflection.Emit;

namespace ORS_ER
{
    internal class Parser
    {
        public static Task<string> ParseAsync(
            Dictionary<string, Component> PaintItems,
            Dictionary<string, Connection> connections,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() => ParseCore(PaintItems, connections, cancellationToken), cancellationToken);
        }

        private static string ParseCore(
            Dictionary<string, Component> PaintItems,
            Dictionary<string, Connection> connections,
            CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine();

            List<Component> startNodes = PaintItems
                .Where(kv => kv.Value.Inputs.Count == 0)
                .Select(kv => kv.Value)
                .ToList();

            while (startNodes.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentNode = startNodes[0];
                startNodes.RemoveAt(0);

                currentNode.GenerateCode();
                sb.AppendLine(currentNode.Code);

                var outputConnections = connections.Values
                    .Where(c => c.fromComponentId == currentNode.GetId())
                    .ToList();

                foreach (var conn in outputConnections)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var nextNode = PaintItems[conn.toComponentId];
                    nextNode.Inputs[conn.toId].name = currentNode.Outputs[conn.fromId].name;
                    nextNode.Inputs[conn.toId].value = currentNode.Outputs[conn.fromId].value;

                    if (!startNodes.Contains(nextNode))
                    {
                        startNodes.Add(nextNode);
                    }

                }
            }

            return sb.ToString();
        }

        public static Task EvaluateAsync(string code, CancellationToken cancellationToken = default)
        {
            var options = ScriptOptions.Default
                .WithReferences(
                    typeof(object).Assembly,
                    typeof(Enumerable).Assembly,
                    typeof(Console).Assembly)
                .WithImports("System", "System.Linq", "System.Collections.Generic", "System.Text");

            return CSharpScript.RunAsync(code, options, globals: null, cancellationToken: cancellationToken);
        }
    }
}
