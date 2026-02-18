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

namespace ORS_ER
{
    internal class Parser
    {
        public static string ParseCircuitAsync(
    Dictionary<string, Component> PaintItems,
    Dictionary<string, Connection> connections
 )
        {
            var startNodes = new Queue<Component>(
                PaintItems
                    .Where(kv => kv.Value.Inputs.Count() == 0)
                    .Select(kv => kv.Value));
            List<string> done = new List<string>();
            while (startNodes.Count > 0)
            {
                var currentNode = startNodes.Dequeue();
                if (done.Contains(currentNode.GetId()))
                    continue;
                done.Add(currentNode.GetId());

                List<bool> vals = new();
                foreach (var io in currentNode.Inputs.Values)
                {
                    bool val = false;
                    try
                    {
                        dynamic tmp = PaintItems[connections[io.inputConnectionIds.First()].fromComponentId].Value.Item2;
                        if (tmp is bool)
                        {
                            val = tmp;
                        }
                        else
                        {
                            var fromId = connections[io.inputConnectionIds.First()].fromId;
                            var index = PaintItems[connections[io.inputConnectionIds.First()].fromComponentId].Outputs[fromId].IfTrue;
                            val = tmp[int.Parse(index)];
                        }
                    }
                    catch (Exception ex)
                    {
                        val = false;
                    }
                    vals.Add(val);
                }

                try
                {
                    currentNode.GenerateCode(vals);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} error: {ex.Message}");
                    return "";
                }

                var outputConnections = connections.Values
                    .Where(c => c.fromComponentId == currentNode.GetId())
                    .ToList();

                foreach (var conn in outputConnections)
                {
                    var nextNode = PaintItems[conn.toComponentId];
                    startNodes.Enqueue(nextNode);

                }
            }

            return "";
        }

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
                    .Where(kv => kv.Value.Inputs.First().Value.inputConnectionIds.Count() == 0)
                    .Select(kv => kv.Value));
            var queuedNodes = new HashSet<string>(startNodes.Select(node => node.GetId()));

            Debug.WriteLine(startNodes.Count());
            Debug.WriteLine(queuedNodes.Count());

            while (startNodes.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentNode = startNodes.Dequeue();
                queuedNodes.Remove(currentNode.GetId());

                try
                {
                    currentNode.GenerateCode();
                }
                catch (DivideByZeroException ex)
                {
                    currentNode.IsBroken = true;
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} divide by zero error: \n{ex.Message}");
                    return "";
                }
                catch (NullReferenceException ex)
                {
                    currentNode.IsBroken = true;
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} null reference error: \n{ex.Message}");
                    return "";
                }
                catch (ArgumentException ex)
                {
                    currentNode.IsBroken = true;
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} argument error: \n{ex.Message}");
                    return "";
                }
                catch (InvalidOperationException ex)
                {
                    currentNode.IsBroken = true;
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} invalid operation error: \n{ex.Message}");
                    return "";
                }
                catch (Exception ex)
                {
                    currentNode.IsBroken = true;
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} error");
                    return "";
                }

                var outputConnections = connections.Values
                    .Where(c => c.fromComponentId == currentNode.GetId())
                    .ToList();

                if (currentNode is If or While)
                {
                    outputConnections = connections.Values.Where(c => c.fromComponentId == currentNode.GetId() && currentNode.Outputs.Values.Where(kv => kv.GetId() == c.fromId && kv.IfTrue != "").ToList().Count() > 0).ToList();
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
    }
}
