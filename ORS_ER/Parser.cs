using ORS_ER.components;
using ORS_ER.connections;

namespace ORS_ER
{
    internal class Parser
    {
        public static void RunCircuitSimulation(
    Dictionary<string, Component> paintItems,
    Dictionary<string, Connection> connections
 )
        {
            var queuedNodes = new Queue<Component>(
                paintItems
                    .Where(kv => kv.Value.Inputs.Count() == 0)
                    .Select(kv => kv.Value));
            List<string> processedNodes = new List<string>();
            while (queuedNodes.Count > 0)
            {
                var currentNode = queuedNodes.Dequeue();
                if (processedNodes.Contains(currentNode.GetId()))
                    continue;
                processedNodes.Add(currentNode.GetId());

                List<bool> inputValues = new();
                foreach (var inputs in currentNode.Inputs.Values)
                {
                    bool vallueToAdd = false;
                    try
                    {
                        dynamic inputValue = paintItems[connections[inputs.inputConnectionIds.First()].fromComponentId].Value.Item2;
                        if (inputValue is bool)
                        {
                            vallueToAdd = inputValue;
                        }
                        else
                        {
                            var fromId = connections[inputs.inputConnectionIds.First()].fromId;
                            var index = paintItems[connections[inputs.inputConnectionIds.First()].fromComponentId].Outputs[fromId].IfTrue;
                            vallueToAdd = inputValue[int.Parse(index)];
                        }
                    }
                    catch (Exception ex)
                    {
                        vallueToAdd = false;
                    }
                    inputValues.Add(vallueToAdd);
                }

                try
                {
                    currentNode.RunInternalSimulation(inputValues);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} error: {ex.Message}");
                    return;
                }

                var outputConnections = connections.Values
                    .Where(c => c.fromComponentId == currentNode.GetId())
                    .ToList();

                foreach (var conn in outputConnections)
                {
                    var nextNode = paintItems[conn.toComponentId];
                    queuedNodes.Enqueue(nextNode);

                }
            }
        }

        public static void ParseFlowchartAsync(
            Dictionary<string, Component> paintItems,
            Dictionary<string, Connection> connections,
            CancellationToken cancellationToken = default)
        {
            Task.Run(() => ParseFlowchartCore(paintItems, connections, cancellationToken), cancellationToken);
        }

        private static void ParseFlowchartCore(
            Dictionary<string, Component> paintItems,
            Dictionary<string, Connection> connections,
            CancellationToken cancellationToken)
        {
            var queuedNodes = new Queue<Component>(
                paintItems
                    .Where(kv => kv.Value.Inputs.First().Value.inputConnectionIds.Count() == 0 && (kv.Value.GetType() == typeof(Input) || kv.Value.GetType() == typeof(Operator)))
                    .Select(kv => kv.Value));
            var nodeIdHashed = new HashSet<string>(queuedNodes.Select(node => node.GetId()));

            while (queuedNodes.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentNode = queuedNodes.Dequeue();
                nodeIdHashed.Remove(currentNode.GetId());

                try
                {
                    currentNode.GenerateCode();
                }
                catch (DivideByZeroException ex)
                {
                    currentNode.IsBroken = true;
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} divide by zero error: \n{ex.Message}");
                    return;
                }
                catch (NullReferenceException ex)
                {
                    currentNode.IsBroken = true;
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} null reference error: \n{ex.Message}");
                    return;
                }
                catch (ArgumentException ex)
                {
                    currentNode.IsBroken = true;
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} argument error: \n{ex.Message}");
                    return;
                }
                catch (InvalidOperationException ex)
                {
                    currentNode.IsBroken = true;
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} invalid operation error: \n{ex.Message}");
                    return;
                }
                catch (Exception ex)
                {
                    currentNode.IsBroken = true;
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} error");
                    return;
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
                    var nextNode = paintItems[conn.toComponentId];
                    if (nodeIdHashed.Add(nextNode.GetId()))
                        queuedNodes.Enqueue(nextNode);
                }
            }
        }
    }
}
