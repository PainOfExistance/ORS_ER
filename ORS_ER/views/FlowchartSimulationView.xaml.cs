using Microsoft.Win32;
using ORS_ER.components;
using ORS_ER.connections;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ORS_ER.views;

public partial class FlowchartSimulationView : UserControl
{
    private TextWriter? _previousOut;
    private TextWriter? _previousError;
    private bool _consoleRedirected;
    private Task? _simulationTask;
    private readonly object _simulationLock = new();
    private Component _copyElement;
    private SKPoint _panOffset = new(0, 0);
    private float _zoom = 1.0f;
    private bool _isPanning;
    private bool _isMoving;
    private SKPoint _panStartMouse;
    private SKPoint _panStartOffset;
    private SKPoint _mouseWorld;
    private const float MinZoom = 0.1f;
    private const float MaxZoom = 10.0f;
    private const float ZoomStep = 1.1f;
    private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Input);
    public string LoadedFilePath = "";
    private const float PalettePreviewZoom = 0.225f;
    public List<(Dictionary<string, Component>, Dictionary<string, Connection>)> history = new();
    private int _histroyPointer = 0;

    public ObservableCollection<Component> Items { get; } = new()
    {
        new Input("String Input", "String input.", "Inputs"),
        new Input("Numerical Input", "Numerical input.", "Inputs"),
        new Input("Binary Input", "Binary input.", "Inputs"),
        new Input("Array Input", "Array input.", "Inputs"),
        new Print("Print", "Prints to console.", "Outputs"),
        new Operator("Operator Block", "Performs numerical or bolean or string operations.", "Logic"),
        new ArrayOperator("Array Operator", "Performs array operations.", "Logic"),
        new If("If", "Branches based on condition.", "Control Flow"),
        new While("While", "Repeats based on condition.", "Control Flow"),
    };

    public Dictionary<string, Component> PaintItems { get; set; } = new();

    public Dictionary<string, Connection> Connections { get; set; } = new();
    private bool _isConnecting;
    private string _connectingConnectionId = "";
    public CancellationTokenSource cts = new();

    public FlowchartSimulationView()
    {
        InitializeComponent();
        DataContext = this;

        Loaded += FlowchartSimulationView_Loaded;
        Unloaded += FlowchartSimulationView_Unloaded;
        PreviewMouseRightButtonDown += FlowchartSimulationView_PreviewMouseRightButtonDown;
        Focusable = true;
        history.Add(new(new Dictionary<string, Component>(), new Dictionary<string, Connection>()));
    }

    private void AddToHistory()
    {
        if (history.Count > 30)
        {
            history.RemoveAt(0);
        }

        if (_histroyPointer != history.Count - 1)
        {
            history = history.Take(_histroyPointer + 1).ToList();
        }

        var paintItemsCopy = PaintItems.ToDictionary(
            kv => kv.Key,
            kv => (Component)kv.Value);
        var connectionsCopy = Connections.ToDictionary(
            kv => kv.Key,
            kv => (Connection)kv.Value);
        history.Add((paintItemsCopy, connectionsCopy));
        _histroyPointer = _histroyPointer + 1;
    }

    private void FlowchartSimulationView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_consoleRedirected)
            return;

        _previousOut = Console.Out;
        _previousError = Console.Error;
        var uiWriter = new UiTextBlockWriter(ConsoleOutput);
        Console.SetOut(TextWriter.Synchronized(uiWriter));
        Console.SetError(TextWriter.Synchronized(uiWriter));
        _consoleRedirected = true;
    }

    private void FlowchartSimulationView_Unloaded(object sender, RoutedEventArgs e)
    {
        if (!_consoleRedirected)
            return;

        if (_previousOut is not null)
            Console.SetOut(_previousOut);
        if (_previousError is not null)
            Console.SetError(_previousError);

        _previousOut = null;
        _previousError = null;
        _consoleRedirected = false;
    }

    public void FocusCanvas()
    {
        skiaElement.Focus();
        Focus();
    }

    public void ClearPaletteSelection() => LayersListView.SelectedItem = null;

    public void Invalidate() => skiaElement.InvalidateVisual();

    private void PalettePreviewElement_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not SKElement el)
            return;

        el.PaintSurface += PalettePreview_OnPaintSurface;
        el.InvalidateVisual();
    }

    private void FlowchartSimulationView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        skiaElement.Cursor = Cursors.Arrow;
        CancelPendingConnection(true, true);
    }

    public void Delete(KeyEventArgs e)
    {
        if (TryDeleteSelectedConnection())
        {
            TriggerSimulationRun();
            CompleteDelete(e);
            AddToHistory();
            return;
        }

        if (TryDeleteSelectedComponent())
        {
            TriggerSimulationRun();
            CompleteDelete(e);
            AddToHistory();
            return;
        }

        e.Handled = true;
    }

    public void ClearNestedConnectionScopes(string scopeId, List<string> toVisit)
    {
        try
        {
            // Traverse outgoing connections and clear nested If/While scope flags.
            for (; toVisit.Count > 0;)
            {
                string current = toVisit.First();
                toVisit.RemoveAt(0);
                if (PaintItems[current].IsInsideIf.Contains(scopeId) && PaintItems[current].IsInsideIf != "")
                {
                    PaintItems[current].IsInsideIf = "";
                    foreach (string outputKey in PaintItems[current].Outputs.Keys)
                    {
                        for (int i = 0; i < PaintItems[current].Outputs[outputKey].OutputConnectionIds.Count; i++)
                        {
                            toVisit.Add(Connections[PaintItems[current].Outputs[outputKey].OutputConnectionIds[i]].ToComponentId);
                        }
                    }
                }

                if (PaintItems[current].IsInsideWhile.Contains(scopeId) && PaintItems[current].IsInsideWhile != "")
                {
                    PaintItems[current].IsInsideWhile = "";
                    foreach (string outputKey in PaintItems[current].Outputs.Keys)
                    {
                        for (int i = 0; i < PaintItems[current].Outputs[outputKey].OutputConnectionIds.Count; i++)
                        {
                            toVisit.Add(Connections[PaintItems[current].Outputs[outputKey].OutputConnectionIds[i]].ToComponentId);
                        }
                    }
                }

                if (PaintItems[current] is If)
                {
                    string nextIsInsideIfItem = "";
                    if (PaintItems[current].Outputs.First().Value.OutputConnectionIds.Count() > 0)
                    {
                        nextIsInsideIfItem = Connections[PaintItems[current].Outputs.First().Value.OutputConnectionIds.First()].ToComponentId;
                    }
                    else if (PaintItems[current].Outputs.Last().Value.OutputConnectionIds.Count() > 0)
                    {
                        nextIsInsideIfItem = Connections[PaintItems[current].Outputs.Last().Value.OutputConnectionIds.First()].ToComponentId;
                    }

                    bool stoping = false;
                    while (!stoping)
                    {
                        if (PaintItems[nextIsInsideIfItem].IsInsideIf.Contains(PaintItems[current].GetId()))
                        {
                            if (PaintItems[nextIsInsideIfItem].Outputs.First().Value.OutputConnectionIds.Count > 0)
                            {
                                nextIsInsideIfItem = Connections[PaintItems[nextIsInsideIfItem].Outputs.First().Value.OutputConnectionIds.First()].ToComponentId;
                            }
                            else
                            {
                                nextIsInsideIfItem = "";
                                stoping = true;
                            }
                        }
                        else
                        {
                            stoping = true;
                            //nextIsInsideIfItem = Connections[PaintItems[nextIsInsideIfItem].Outputs.First().Value.OutputConnectionIds.First()].ToComponentId;
                        }
                    }
                    if (nextIsInsideIfItem != "")
                    {
                        //nextIsInsideIfItem = Connections[PaintItems[nextIsInsideIfItem].Outputs.First().Value.OutputConnectionIds.First()].ToComponentId;
                        PaintItems[nextIsInsideIfItem].IsInsideIf = "";
                        if (PaintItems[nextIsInsideIfItem].Outputs.First().Value.OutputConnectionIds.Count() > 0)
                        {
                            toVisit.Add(Connections[PaintItems[nextIsInsideIfItem].Outputs.First().Value.OutputConnectionIds.First()].ToComponentId);
                        }
                    }
                }
                else if (PaintItems[current] is While)
                {
                    var falseOutput = PaintItems[current].Outputs
                        .Where(kv => kv.Value.IfTrue == "False")
                        .Select(kv => kv.Value.OutputConnectionIds.FirstOrDefault())
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(falseOutput) && Connections.TryGetValue(falseOutput, out var whileConnection))
                    {
                        toVisit.Add(whileConnection.ToComponentId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error during scope clearing: " + ex.Message);
        }
    }

    private void PropagateIsInsideFlags(Component component)
    {
        try
        {
            // Traverse outgoing connections and propagate If/While scope flags.
            var toVisit = new List<string> { component.GetId() };
            string prevIsInsideIf = component.IsInsideIf;
            string prevIsInsideWhile = component.IsInsideWhile;
            for (; toVisit.Count > 0;)
            {
                string current = toVisit.First();
                toVisit.RemoveAt(0);
                if (PaintItems[current].Inputs.Last().Value.InputConnectionIds.Count() == 2)
                {
                    // todo fix this for if inside if propagation
                    break;
                }
                else if (prevIsInsideIf != "")
                {
                    PaintItems[current].IsInsideIf = prevIsInsideIf;
                }
                else if (prevIsInsideWhile != "")
                {
                    PaintItems[current].IsInsideWhile = prevIsInsideWhile;
                }

                foreach (string outputKey in PaintItems[current].Outputs.Keys)
                {
                    for (int i = 0; i < PaintItems[current].Outputs[outputKey].OutputConnectionIds.Count; i++)
                    {
                        if (PaintItems[current].GetType() == typeof(If) && PaintItems[current].Outputs[outputKey].IfTrue == "True")
                        {
                            string nextIsInsideIfItem = Connections[PaintItems[current].Outputs[outputKey].OutputConnectionIds[i]].ToComponentId;
                            bool stoping = false;
                            while (!stoping)
                            {
                                if (PaintItems[nextIsInsideIfItem].IsInsideIf.Contains(PaintItems[current].GetId()))
                                {
                                    if (PaintItems[nextIsInsideIfItem].Outputs.First().Value.OutputConnectionIds.Count > 0)
                                    {
                                        nextIsInsideIfItem = Connections[PaintItems[nextIsInsideIfItem].Outputs.First().Value.OutputConnectionIds.First()].ToComponentId;
                                    }
                                    else
                                    {
                                        nextIsInsideIfItem = "";
                                        stoping = true;
                                    }
                                }
                                else
                                {
                                    stoping = true;
                                    //nextIsInsideIfItem = Connections[PaintItems[nextIsInsideIfItem].Outputs.First().Value.OutputConnectionIds.First()].ToComponentId;
                                }
                            }
                            if (nextIsInsideIfItem != "")
                            {
                                if (prevIsInsideIf != "")
                                {
                                    PaintItems[nextIsInsideIfItem].IsInsideIf = prevIsInsideIf;
                                }
                                else if (prevIsInsideWhile != "")
                                {
                                    PaintItems[nextIsInsideIfItem].IsInsideWhile = prevIsInsideWhile;
                                }
                                if (PaintItems[nextIsInsideIfItem].Outputs.First().Value.OutputConnectionIds.Count() > 0)
                                {
                                    toVisit.Add(Connections[PaintItems[nextIsInsideIfItem].Outputs.First().Value.OutputConnectionIds.First()].ToComponentId);
                                }
                            }
                        }
                        else if (PaintItems[current].GetType() == typeof(While) && PaintItems[current].Outputs[outputKey].IfTrue == "False")
                        {
                            toVisit.Add(Connections[PaintItems[current].Outputs[outputKey].OutputConnectionIds[i]].ToComponentId);
                        }
                        else if (!(PaintItems[current].GetType() == typeof(While) || PaintItems[current].GetType() == typeof(If)))
                        {
                            toVisit.Add(Connections[PaintItems[current].Outputs[outputKey].OutputConnectionIds[i]].ToComponentId);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error during scope propagation: " + ex.Message);
        }
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.WhiteSmoke);

        canvas.Scale(_zoom);
        canvas.Translate(_panOffset);

        // Draw connections first, then components on top.
        foreach (var connection in Connections.Values)
        {
            var fromComponent = PaintItems[connection.FromComponentId];
            var fromNode = fromComponent.Outputs[connection.FromIOId].Node;

            var toPoint = connection.GetId() == _connectingConnectionId
                ? _mouseWorld
                : PaintItems[connection.ToComponentId].Inputs[connection.ToIOId].Node;

            canvas.DrawLine(fromNode, toPoint, connection.IsSelected ? Paints.SelectedLineStroke : Paints.LineStroke);
        }

        foreach (var itemX in PaintItems)
        {
            foreach (var itemY in PaintItems)
            {
                if (itemX.Value.Rect.IntersectsWith(itemY.Value.Rect) && itemX.Key != itemY.Key)
                {
                    itemY.Value.OffsetRect((int)itemY.Value.prevXY.X, (int)itemY.Value.prevXY.Y);
                }
            }
        }

        foreach (var item in PaintItems)
            item.Value.Paint(canvas);
    }


    private SKPoint ScreenToWorld(SKPoint screen) => new(screen.X / _zoom - _panOffset.X, screen.Y / _zoom - _panOffset.Y);

    private SKPoint ToScreenPoint(Point position)
    {
        var dpi = VisualTreeHelper.GetDpi(skiaElement);
        return new SKPoint((float)(position.X * dpi.DpiScaleX), (float)(position.Y * dpi.DpiScaleY));
    }

    private void PalettePreview_OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(new SKColor(0xF2, 0xF2, 0xF2));

        if (sender is not SKElement element || element.Tag is not Component component)
            return;

        DrawPalettePreview(canvas, e.Info.Width, e.Info.Height, component);
    }

    private static void DrawPalettePreview(SKCanvas canvas, int width, int height, Component component)
    {
        float pad = 4;
        var clip = new SKRoundRect(new SKRect(pad, pad, width - pad, height - pad), 6, 6);

        using var border = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            Color = new SKColor(0x22, 0x00, 0x00, 0x00),
            StrokeWidth = 1
        };

        canvas.Save();
        canvas.ClipRoundRect(clip, SKClipOperation.Intersect, true);

        var clone = Creator.Create(component.Name, component.Description, component.Category, 0, 0);

        var content = SKRect.Create(pad, pad, width - (pad * 2), height - (pad * 2));
        canvas.Translate(content.MidX, content.MidY);
        canvas.Scale(PalettePreviewZoom);

        clone.Paint(canvas);
        canvas.Restore();

        canvas.DrawRoundRect(clip, border);
    }

    public void CancelConnection()
    {
        CancelPendingConnection(false, false);
    }

    private void CancelPendingConnection(bool invalidateCanvas, bool log)
    {
        if (!_isConnecting)
            return;

        if (Connections.TryGetValue(_connectingConnectionId, out var prev))
            PaintItems[prev.FromComponentId].Outputs[prev.FromIOId].OutputConnectionIds.Remove(_connectingConnectionId);

        Connections.Remove(_connectingConnectionId);
        _isConnecting = false;
        _connectingConnectionId = "";
        if (log)
            Debug.WriteLine("Cancelled Connection");
        if (invalidateCanvas)
            skiaElement.InvalidateVisual();
    }

    private void RemoveConnection(string connectionId, Connection connection)
    {
        if (PaintItems.TryGetValue(connection.FromComponentId, out var fromComponent))
            fromComponent.Outputs[connection.FromIOId].OutputConnectionIds.Remove(connectionId);

        if (!string.IsNullOrWhiteSpace(connection.ToComponentId)
            && !string.IsNullOrWhiteSpace(connection.ToIOId)
            && PaintItems.TryGetValue(connection.ToComponentId, out var toComponent))
            toComponent.Inputs[connection.ToIOId].InputConnectionIds.Remove(connectionId);

        Connections.Remove(connectionId);
    }

    private void RemoveInputConnections(Component component)
    {
        foreach (var input in component.Inputs.Values)
        {
            foreach (var id in input.InputConnectionIds.ToArray())
            {
                if (!Connections.TryGetValue(id, out var conn))
                    continue;

                RemoveConnection(id, conn);
                Debug.WriteLine("Deleted Connection");
            }

            input.InputConnectionIds.Clear();
        }
    }

    private void RemoveOutputConnections(Component component)
    {
        List<string> toRemove = new List<string>();
        foreach (var output in component.Outputs.Values)
        {
            foreach (var id in output.OutputConnectionIds.ToArray())
            {
                if (!Connections.TryGetValue(id, out var conn))
                    continue;

                toRemove.Add(conn.ToComponentId);
                RemoveConnection(id, conn);
                Debug.WriteLine("Deleted Connection");
            }

            output.OutputConnectionIds.Clear();
        }
    }

    private void RemoveComponent(string componentId, Component component)
    {
        var toRemove = new List<string>();
        RemoveInputConnections(component);

        if (component.GetType() == typeof(While))
        {
            component.IsInsideWhile = component.GetId();
        }
        else if (component.GetType() == typeof(If))
        {
            component.IsInsideIf = component.GetId();
        }

        toRemove.Add(componentId);
        ClearNestedConnectionScopes(component.IsInsideIf, toRemove);

        toRemove.Add(componentId);
        ClearNestedConnectionScopes(component.IsInsideWhile, toRemove);
        RemoveOutputConnections(component);
        PaintItems.Remove(componentId);
    }

    private bool TryDeleteSelectedConnection()
    {
        foreach (var connectionEntry in Connections.ToArray())
        {
            if (!connectionEntry.Value.IsSelected)
                continue;

            var toRemove = new List<string>();
            var toComponent = PaintItems[connectionEntry.Value.ToComponentId];

            if (toComponent.IsInsideIf != "")
            {
                toRemove.Add(connectionEntry.Value.ToComponentId);
                ClearNestedConnectionScopes(toComponent.IsInsideIf, toRemove);
            }

            if (toComponent.IsInsideWhile != "")
            {
                toRemove.Add(connectionEntry.Value.ToComponentId);
                ClearNestedConnectionScopes(toComponent.IsInsideWhile, toRemove);
            }

            RemoveConnection(connectionEntry.Key, connectionEntry.Value);
            Debug.WriteLine("Deleted Connection");
            return true;
        }

        return false;
    }

    private bool TryDeleteSelectedComponent()
    {
        foreach (var itemEntry in PaintItems.ToArray())
        {
            if (!itemEntry.Value.Selected)
                continue;

            RemoveComponent(itemEntry.Key, itemEntry.Value);
            Debug.WriteLine("Deleted Component");
            return true;
        }

        return false;
    }

    private void CompleteDelete(KeyEventArgs e)
    {
        skiaElement.InvalidateVisual();
        e.Handled = true;
    }

    private (HitTarget, Component, IO)? HitTest(SKPoint world, MouseButton mouseButton)
    {
        (HitTarget, Component, IO)? hitResult = null;
        (HitTarget, Component, IO)? candidateResult = null;
        foreach (Component item in PaintItems.Values)
        {
            item.Selected = false;
            Dictionary<string, dynamic> variables = Parser.ExtractVariables(PaintItems, Connections, item);
            candidateResult = item.HitTest(world, variables, mouseButton);
            if (candidateResult != null)
            {
                if (candidateResult.Value.Item1 == HitTarget.Output)
                {
                    // Hitting output starts connecting.
                    if (!_isConnecting && item.Outputs[candidateResult.Value.Item3.GetId()].OutputConnectionIds.Count() < 1)
                    {
                        _isConnecting = true;
                        Connection newConnection = new Connection(candidateResult.Value.Item3.GetId(), "", candidateResult.Value.Item2.GetId(), "");
                        _connectingConnectionId = newConnection.GetId();
                        Connections.Add(_connectingConnectionId, newConnection);

                        item.Outputs[candidateResult.Value.Item3.GetId()].OutputConnectionIds.Add(_connectingConnectionId);
                        hitResult = candidateResult;
                        return hitResult;
                    }
                    CancelPendingConnection(false, false);
                    hitResult = candidateResult;
                    return hitResult;
                }
                if (candidateResult.Value.Item1 == HitTarget.Input)
                {
                    try
                    {
                        bool clearId = false;

                        if (item is While && item.Inputs.First().Value.GetId() == candidateResult.Value.Item3.GetId())
                        {
                            // while loop back only if inside repeat branch of while or if the while has multiple connections to the input (indicating it's a nested loop)
                            if (!PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile.Contains(item.GetId()) ||
                                candidateResult.Value.Item3.InputConnectionIds.Count > 0 ||
                                PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromIOId].OutputConnectionIds.Count > 1)
                            {
                                CancelConnection();
                                hitResult = candidateResult;
                                return hitResult;
                            }
                        }
                        else if (item is While && PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile.Contains(candidateResult.Value.Item2.GetId()) && candidateResult.Value.Item3.IfTrue == "Start")
                        {
                            //allow only connections from inside the loop to the start of the loop body to allow nested loops
                            CancelConnection();
                            hitResult = candidateResult;
                            return hitResult;

                        }
                        else if (item.IsInsideIf != "" &&
                                 PaintItems[Connections[_connectingConnectionId].FromComponentId] is If &&
                                 PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromIOId].OutputConnectionIds.Count == 1 &&
                                  candidateResult.Value.Item3.InputConnectionIds.Count == 1)
                        {
                            //skip connection from T/F to outside of it block connection needed
                            clearId = true;
                        }
                        else if (item.IsInsideIf != "" &&
                                 item.IsInsideIf.Contains(PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf.Split("_")[0]) &&
                                 PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf.Split("_")[1] != item.IsInsideIf.Split("_")[1] &&
                                 PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromIOId].OutputConnectionIds.Count == 1)
                        {
                            //if termination allowed
                            clearId = true;
                        }
                        else if (item.IsInsideIf != "" || item.IsInsideWhile != "")
                        {
                            //outside connection forbiden
                            CancelConnection();
                            hitResult = candidateResult;
                            return hitResult;
                        }
                        else if ((PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf != "" || PaintItems[Connections[_connectingConnectionId].FromComponentId] is If) &&
                                 PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromIOId].OutputConnectionIds.Count > 1)
                        {
                            //if outside branching forbiden
                            CancelConnection();
                            hitResult = candidateResult;
                            return hitResult;
                        }
                        else if ((PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile != "" || PaintItems[Connections[_connectingConnectionId].FromComponentId] is While) &&
                                 PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromIOId].OutputConnectionIds.Count > 1)
                        {
                            //while outside branching forbiden
                            CancelConnection();
                            hitResult = candidateResult;
                            return hitResult;
                        }
                        else if ((item.IsInsideIf != "" || item.IsInsideWhile != "") && item.Inputs[Connections[_connectingConnectionId].ToIOId].InputConnectionIds.Count > 0)
                        {
                            //incesting in if forbiden
                            CancelConnection();
                            hitResult = candidateResult;
                            return hitResult;
                        }
                        else if ((PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile != "" || PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf != "") && candidateResult.Value.Item3.InputConnectionIds.Count() > 0)
                        {
                            //cancel if/while branching to already branched input to avoid incest
                            CancelConnection();
                            hitResult = candidateResult;
                            return hitResult;
                        }

                        if (candidateResult.Value.Item3.InputConnectionIds.Count() == 1 && !clearId)
                        {
                            //forbid multiconnections to inputs
                            CancelConnection();
                            hitResult = candidateResult;
                            return hitResult;
                        }

                        if (_isConnecting && Connections[_connectingConnectionId].FromComponentId != candidateResult.Value.Item2.GetId())
                        {
                            if (PaintItems[Connections[_connectingConnectionId].FromComponentId] is If && !clearId)
                            {
                                string sufix = PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromIOId].GetId() == PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs.First().Value.GetId() ? "_False" : "_True";
                                item.IsInsideIf = Connections[_connectingConnectionId].FromComponentId + sufix;
                            }
                            else if (PaintItems[Connections[_connectingConnectionId].FromComponentId] is While && PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromIOId].IfTrue != "False")
                            {
                                item.IsInsideWhile = Connections[_connectingConnectionId].FromComponentId + "_" + PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromIOId].IfTrue;
                            }
                            else
                            {
                                if (clearId)
                                {
                                    //propagate IsInside when ending while/if
                                    if (PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf.Split("_")[0] != "")
                                    {
                                        if (PaintItems[Connections[_connectingConnectionId].FromComponentId] is If ||
                                            PaintItems[Connections[_connectingConnectionId].FromComponentId] is While)
                                        {
                                            item.IsInsideIf = PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf.Split("_")[0];
                                        }
                                        else
                                        {
                                            item.IsInsideIf = PaintItems[PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf.Split("_")[0]].IsInsideIf;
                                        }
                                    }
                                    else if (PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile.Split("_")[0] != "")
                                    {
                                        if (PaintItems[Connections[_connectingConnectionId].FromComponentId] is If ||
                                            PaintItems[Connections[_connectingConnectionId].FromComponentId] is While)
                                        {
                                            item.IsInsideWhile = PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile.Split("_")[0];
                                        }
                                        else
                                        {
                                            item.IsInsideWhile = PaintItems[PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile.Split("_")[0]].IsInsideWhile;
                                        }
                                    }
                                }
                                else if (!(item is While && PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile.Contains(item.GetId())))
                                {
                                    //set IsInside when connecting 
                                    item.IsInsideIf = PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf;
                                    item.IsInsideWhile = PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile;
                                }
                            }

                            PropagateIsInsideFlags(item);
                            Connections[_connectingConnectionId].ToIOId = candidateResult.Value.Item3.GetId();
                            Connections[_connectingConnectionId].ToComponentId = candidateResult.Value.Item2.GetId();
                            item.Inputs[candidateResult.Value.Item3.GetId()].InputConnectionIds.Add(_connectingConnectionId);
                            Connections[_connectingConnectionId].IsSelected = false;
                            _isConnecting = false;
                            _connectingConnectionId = "";
                            TriggerSimulationRun();
                            AddToHistory();
                        }
                        else if (_isConnecting)
                        {
                            CancelConnection();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error during connection: " + ex.Message);
                        CancelConnection();
                    }

                    hitResult = candidateResult;
                }
                if (candidateResult.Value.Item1 == HitTarget.Rect)
                {
                    item.Selected = true;
                    hitResult = candidateResult;
                    if (_isConnecting)
                    {
                        CancelPendingConnection(false, false);
                    }
                }
                if (candidateResult.Value.Item1 == HitTarget.Button)
                {
                    if (_isConnecting)
                    {
                        CancelPendingConnection(false, false);
                    }
                    hitResult = candidateResult;
                }
            }
        }
        return hitResult;
    }

    private void SkiaElement_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        skiaElement.Focus();
        var mousePosition = e.GetPosition(skiaElement);
        var mouseScreen = ToScreenPoint(mousePosition);
        _mouseWorld = ScreenToWorld(mouseScreen);
        (HitTarget, Component, IO)? hit = HitTest(_mouseWorld, e.ChangedButton);

        // Check if we hit connection or we deselect it.
        foreach (var conn in Connections)
        {
            if (conn.Value.ToIOId == "" || hit != null)
            {
                conn.Value.IsSelected = false;
                continue;
            }
            var fromNode = PaintItems[conn.Value.FromComponentId].Outputs[conn.Value.FromIOId].Node;
            var toNode = PaintItems[conn.Value.ToComponentId].Inputs[conn.Value.ToIOId].Node;
            var isSelected = conn.Value.HitTest(_mouseWorld, fromNode, toNode);

            if (isSelected)
            {
                _isPanning = false;
                skiaElement.InvalidateVisual();
                e.Handled = true;
                return;
            }
        }

        if (e.ChangedButton == MouseButton.Right)
        {
            LayersListView.SelectedItem = null;
            _isPanning = false;
            _isMoving = false;
            skiaElement.Cursor = Cursors.Arrow;
            skiaElement.InvalidateVisual();
            return;
        }

        if (hit != null && (hit.Value.Item1 == HitTarget.Rect || hit.Value.Item1 == HitTarget.Button) && e.ChangedButton == MouseButton.Left)
        {
            //Rect moving.
            skiaElement.Cursor = Cursors.Hand;
            _isMoving = true;
            LayersListView.SelectedItem = null;
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (hit != null && hit.Value.Item1 == HitTarget.Button && e.ChangedButton == MouseButton.Right)
        {
            TriggerSimulationRun();
            skiaElement.InvalidateVisual();
            LayersListView.SelectedItem = null;
            e.Handled = true;
            return;
        }

        if (hit != null && (hit.Value.Item1 == HitTarget.Input || hit.Value.Item1 == HitTarget.Output) && e.ChangedButton == MouseButton.Left)
        {
            skiaElement.Cursor = Cursors.Pen;
            LayersListView.SelectedItem = null;
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (LayersListView.SelectedItem != null)
        {
            int index = LayersListView.SelectedIndex;
            var selected = Items[index];
            var newComponent = Creator.Create(selected.Name, selected.Description, selected.Category, (int)_mouseWorld.X, (int)_mouseWorld.Y);
            PaintItems.Add(newComponent.GetId(), newComponent);
            AddToHistory();
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        if ((e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right) && _isConnecting)
        {
            skiaElement.Cursor = Cursors.Arrow;
            CancelPendingConnection(true, false);
            e.Handled = true;
            return;
        }

        _isPanning = true;
        skiaElement.CaptureMouse();
        skiaElement.Cursor = Cursors.SizeAll;

        _panStartMouse = mouseScreen;
        _panStartOffset = _panOffset;

        skiaElement.InvalidateVisual();
        e.Handled = true;
    }

    private void SkiaElement_OnMouseMove(object sender, MouseEventArgs e)
    {
        var mousePosition = e.GetPosition(skiaElement);
        var mouseScreen = ToScreenPoint(mousePosition);
        _mouseWorld = ScreenToWorld(mouseScreen);

        if (_isPanning)
        {
            var deltaScreen = mouseScreen - _panStartMouse;
            _panOffset = _panStartOffset + deltaScreen;
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_isMoving)
        {
            foreach (var item in PaintItems)
            {
                if (item.Value.Selected)
                    item.Value.OffsetRect((int)_mouseWorld.X, (int)_mouseWorld.Y);
            }
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_isConnecting)
        {
            skiaElement.Cursor = Cursors.Pen;
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        skiaElement.Cursor = Cursors.Arrow;
        e.Handled = true;
    }

    private void SkiaElement_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        _isMoving = false;
        skiaElement.ReleaseMouseCapture();
        if (!_isConnecting)
            skiaElement.Cursor = Cursors.Arrow;
        e.Handled = true;
    }

    private void SkiaElement_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var mousePosition = e.GetPosition(skiaElement);
        var mouseScreen = ToScreenPoint(mousePosition);
        var zoomFactor = e.Delta > 0 ? ZoomStep : 1f / ZoomStep;
        var newZoom = Math.Clamp(_zoom * zoomFactor, MinZoom, MaxZoom);

        if (Math.Abs(newZoom - _zoom) < float.Epsilon)
            return;

        var mouseWorldBefore = ScreenToWorld(mouseScreen);
        _zoom = newZoom;

        _panOffset = new SKPoint(mouseScreen.X / _zoom, mouseScreen.Y / _zoom) - mouseWorldBefore;

        skiaElement.InvalidateVisual();
        e.Handled = true;
    }

    private async void TriggerSimulationRun()
    {
        try
        {
            await RunAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error during simulation: " + ex.Message);
        }
    }

    public async Task RunAsync()
    {
        Task? previousTask;
        CancellationTokenSource previousCts;
        CancellationTokenSource nextCts = new();

        lock (_simulationLock)
        {
            previousTask = _simulationTask;
            previousCts = cts;
            if (!previousCts.IsCancellationRequested)
                previousCts.Cancel();

            cts = nextCts;
        }

        if (previousTask is not null && !previousTask.IsCompleted)
        {
            try
            {
                await previousTask;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error during previous simulation: " + ex.Message);
            }
        }

        previousCts.Dispose();

        var cancellationToken = nextCts.Token;

        //ValueRegistry.ClearAllRegistries();
        ConsoleOutput.Clear();
        ConsoleOutput.Text = "------Console Output------";

        foreach (var item in PaintItems.Values)
            item.Reset();

        var simulationTask = Parser.ParseFlowchartAsync(PaintItems, Connections, cancellationToken);

        lock (_simulationLock)
        {
            if (!ReferenceEquals(cts, nextCts))
            {
                nextCts.Cancel();
                nextCts.Dispose();
                return;
            }

            _simulationTask = simulationTask;
        }

        try
        {
            await simulationTask;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (!cancellationToken.IsCancellationRequested)
            skiaElement.InvalidateVisual();
    }

    public void NewDiagram()
    {
        ConsoleOutput.Text = "------Console Output------";

        foreach (var item in PaintItems.Values)
            item.Reset();

        PaintItems.Clear();
        Connections.Clear();
        _histroyPointer = 0;
        history.Clear();
        history.Add(new(new Dictionary<string, Component>(), new Dictionary<string, Connection>()));
        _isConnecting = false;
        _connectingConnectionId = "";
        skiaElement.InvalidateVisual();
    }

    public void SaveDiagram()
    {
        CancelPendingConnection(true, false);
        Creator.Save(PaintItems, Connections, "FlowChart", LoadedFilePath);
    }

    public void SaveDiagramAs()
    {
        CancelPendingConnection(true, false);
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "Json (*.json)|*.json|Show All Files (*.*)|*.*";
        saveFileDialog.FileName = "diagram";
        saveFileDialog.Title = "Save As";
        saveFileDialog.ShowDialog();

        if (saveFileDialog.FileName != "")
        {
            LoadedFilePath = saveFileDialog.FileName;
        }

        Creator.Save(this.PaintItems, this.Connections, "FlowChart", LoadedFilePath);
    }

    public void SaveCanvasAsPng()
    {
        CancelPendingConnection(true, false);

        CanvasExport.SaveAsPng(PaintItems, Connections);
    }

    public bool LoadDiagram()
    {
        CancelPendingConnection(true, false);

        var items = Creator.Load("FlowChart");
        if (items.Item1.Count == 0 || items.Item3 == "fail")
            return false;

        PaintItems.Clear();
        Connections.Clear();
        foreach (var item in items.Item1)
            PaintItems.Add(item.Key, item.Value);
        foreach (var conn in items.Item2)
            Connections.Add(conn.Key, conn.Value);

        LoadedFilePath = items.Item3;
        skiaElement.InvalidateVisual();

        return true;
    }

    public void Duplicate()
    {
        var item = PaintItems.Values.Where(kv => kv.Selected == true);
        if (item.Count() > 0)
        {
            var itm = item.ElementAt(0);
            var newItem = Creator.Create(itm.Name, itm.Description, itm.Category, (int)(itm.Rect.MidX + itm.Rect.Width) + 10, (int)itm.Rect.MidY);
            newItem.Code = itm.Code;
            newItem.Value = itm.Value;
            PaintItems.Add(newItem.GetId(), newItem);
            PaintItems.Values.Where(kv => kv.Selected == true).ElementAt(0).Selected = false;
            AddToHistory();
            skiaElement.InvalidateVisual();
        }
    }

    public void Copy()
    {
        var item = PaintItems.Values.Where(kv => kv.Selected == true);
        if (item.Count() > 0)
        {
            var itm = item.ElementAt(0);
            _copyElement = Creator.Create(itm.Name, itm.Description, itm.Category, (int)(itm.Rect.MidX), (int)itm.Rect.MidY);
            _copyElement.Code = itm.Code;
            _copyElement.Value = itm.Value;
        }
    }

    public void Paste()
    {
        if (_copyElement != null)
        {
            var itm = Creator.Create(_copyElement.Name, _copyElement.Description, _copyElement.Category, (int)_mouseWorld.X, (int)_mouseWorld.Y);
            itm.Code = _copyElement.Code;
            itm.Value = _copyElement.Value;
            PaintItems.Add(itm.GetId(), itm);
            AddToHistory();
            skiaElement.InvalidateVisual();
        }
    }

    public async Task UndoAsync()
    {
        if (_histroyPointer > 0)
        {
            _histroyPointer--;
            var componentsAndConnections = history[_histroyPointer];
            PaintItems = new Dictionary<string, Component>(componentsAndConnections.Item1);
            Connections = new Dictionary<string, Connection>(componentsAndConnections.Item2);
            skiaElement.InvalidateVisual();
            await RunAsync();
        }
    }

    public async Task RedoAsync()
    {
        if (_histroyPointer < history.Count - 1)
        {
            _histroyPointer++;
            var componentsAndConnections = history[_histroyPointer];
            PaintItems = new Dictionary<string, Component>(componentsAndConnections.Item1);
            Connections = new Dictionary<string, Connection>(componentsAndConnections.Item2);
            skiaElement.InvalidateVisual();
            await RunAsync();
        }
    }
}
