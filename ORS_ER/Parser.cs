using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ORS_ER.components;
using ORS_ER.connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            var startNodes = new Queue<Component>(
                PaintItems
                    .Where(kv => kv.Value.Inputs.Count == 0)
                    .Select(kv => kv.Value));
            var queuedNodes = new HashSet<string>(startNodes.Select(node => node.GetId()));

            while (startNodes.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentNode = startNodes.Dequeue();
                queuedNodes.Remove(currentNode.GetId());

                currentNode.GenerateCode();
                var outputConnections = connections.Values
                    .Where(c => c.fromComponentId == currentNode.GetId())
                    .ToList();

                if (currentNode is If or While)
                {
                    outputConnections = connections.Values.Where(c => c.fromComponentId == currentNode.GetId() && currentNode.Outputs.Values.Any(kv => kv.IfTrue != "")).ToList();
                }

                foreach (var conn in outputConnections)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var nextNode = PaintItems[conn.toComponentId];
                    
                    if (queuedNodes.Add(nextNode.GetId()))
                        startNodes.Enqueue(nextNode);

                }
            }

            return "";
        }

        public static Task EvaluateAsync(string code, CancellationToken cancellationToken = default)
        {
            var options = ScriptOptions.Default
                .WithReferences(
                    typeof(object).Assembly,
                    typeof(Enumerable).Assembly,
                    typeof(Console).Assembly,
                    typeof(System.Runtime.CompilerServices.DynamicAttribute).Assembly,
                    typeof(System.Dynamic.DynamicObject).Assembly,
                    typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly)
                .WithImports(
                    "System",
                    "System.Linq",
                    "System.Collections.Generic",
                    "System.Text",
                    "System.Dynamic");

            return CSharpScript.RunAsync(code, options, globals: null, cancellationToken: cancellationToken);
        }
    }
}
