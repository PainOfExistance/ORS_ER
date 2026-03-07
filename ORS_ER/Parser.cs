using ORS_ER.components;
using ORS_ER.connections;

namespace ORS_ER
{
    internal class Parser
    {
        public static void RunCircuitSimulation(
            Dictionary<string, Component> paintItems,
            Dictionary<string, Connection> connections)
        {
            // Start from components with no inputs to propagate values forward.
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
                    // Get values depending on the type of the input connection - if it's a direct boolean or if its list.
                    bool valueToAdd = false;
                    try
                    {
                        dynamic inputValue = paintItems[connections[inputs.InputConnectionIds.First()].FromComponentId].Value.Item2;
                        if (inputValue is bool)
                        {
                            valueToAdd = inputValue;
                        }
                        if (inputValue is not bool)
                        {
                            var fromId = connections[inputs.InputConnectionIds.First()].FromIOId;
                            var index = paintItems[connections[inputs.InputConnectionIds.First()].FromComponentId].Outputs[fromId].IfTrue;
                            valueToAdd = inputValue[int.Parse(index)];
                        }
                    }
                    catch (Exception ex)
                    {
                        valueToAdd = false;
                    }
                    inputValues.Add(valueToAdd);
                }

                try
                {
                    // Evaluate the component with resolved inputs.
                    currentNode.RunInternalSimulation(inputValues);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"In block {currentNode.Name} with id {currentNode.GetId()} error: {ex.Message}");
                    return;
                }

                var outputConnections = connections.Values
                    .Where(c => c.FromComponentId == currentNode.GetId())
                    .ToList();

                foreach (var conn in outputConnections)
                {
                    var nextNode = paintItems[conn.ToComponentId];
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
            // Seed with flowchart entry nodes (inputs/operators) that have no incoming connections.
            var queuedNodes = new Queue<Component>(
                paintItems
                    .Where(kv => kv.Value.Inputs.First().Value.InputConnectionIds.Count() == 0 && kv.Value.GetType() == typeof(Input))
                    .Select(kv => kv.Value));
            var nodeIdHashed = new HashSet<string>(queuedNodes.Select(node => node.GetId()));
            if (queuedNodes.Count > 1)
            {
                Console.WriteLine("Multiple entry points detected. Please ensure there is only one input block with no incoming connections.");
                return;
            }

            while (queuedNodes.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentNode = queuedNodes.Dequeue();
                nodeIdHashed.Remove(currentNode.GetId());

                try
                {
                    // Generate code for the current block and mark errors on the block itself.
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
                    .Where(c => c.FromComponentId == currentNode.GetId())
                    .ToList();

                if (currentNode is If or While)
                {
                    // Only follow the conditional output path when traversing If/While blocks.
                    outputConnections = connections.Values.Where(c => c.FromComponentId == currentNode.GetId() && currentNode.Outputs.Values.Where(kv => kv.GetId() == c.FromIOId && kv.IfTrue != "").ToList().Count() > 0).ToList();
                }

                foreach (var conn in outputConnections)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var nextNode = paintItems[conn.ToComponentId];
                    if (nodeIdHashed.Add(nextNode.GetId()))
                        queuedNodes.Enqueue(nextNode);
                }
            }
        }

        public static Dictionary<string, dynamic> ExtractVariables(Dictionary<string, Component> paintItems, Dictionary<string, Connection> connections, Component currentItem)
        {
            Dictionary<string, dynamic> variables = new Dictionary<string, dynamic?>();
            Queue<Component> toVisit = new Queue<Component>();
            toVisit.Enqueue(currentItem);
            while (toVisit.Count > 0)
            {
                var item = toVisit.Dequeue();
                if (item is Input || item is Operator)
                {
                    if (item.Value.Item1 != null && item.Value.Item2 != null)
                        variables[item.Value.Item1] = item.Value.Item2;
                }
                var inputConnections = item.Inputs.Values.SelectMany(input => input.InputConnectionIds)
                    .Select(connId => connections[connId])
                    .ToList();


                if (inputConnections.Count >= 2)
                {
                    if (item is While)
                    {
                        inputConnections = paintItems[paintItems[connections[item.Inputs.Last().Value.InputConnectionIds.First()].FromComponentId].IsInsideIf.Split("_")[0]].Inputs.Values
                        .SelectMany(input => input.InputConnectionIds)
                        .Select(connId => connections[connId])
                        .ToList();
                    }
                    else
                    {
                        inputConnections = paintItems[paintItems[connections[item.Inputs.First().Value.InputConnectionIds.First()].FromComponentId].IsInsideIf.Split("_")[0]].Inputs.Values.SelectMany(input => input.InputConnectionIds)
                        .Select(connId => connections[connId])
                        .ToList();
                    }
                }
                else if (item is While)
                {
                    inputConnections = item.Inputs.Values
                        .Where(kv => kv.IfTrue == "Start")
                        .SelectMany(kv => kv.InputConnectionIds.Select(id => connections[id]))
                        .ToList();
                }


                foreach (var conn in inputConnections)
                {
                    var fromComponent = paintItems[conn.FromComponentId];
                    toVisit.Enqueue(fromComponent);
                }
            }
            return variables;
        }
    }
}
