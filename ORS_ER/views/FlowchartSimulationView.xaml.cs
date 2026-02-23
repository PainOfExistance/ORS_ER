using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ORS_ER.components;
using ORS_ER.connections;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace ORS_ER.views;

public partial class FlowchartSimulationView : UserControl
{
    private TextWriter? _previousOut;
    private TextWriter? _previousError;
    private bool _consoleRedirected;

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

    private const float PalettePreviewZoom = 0.225f;


    public ObservableCollection<Component> Items { get; } = new()
    {
        new Input("String Input", "String input.", "Inputs"),
        new Input("Numerical Input", "Numerical input.", "Inputs"),
        new Input("Binary Input", "Binary input.", "Inputs"),
        new Print("Print", "Prints to console.", "Outputs"),
        new Operator("Operator Block", "Performs numerical or bolean or string operations.", "Logic"),
        new If("If", "Branches based on condition.", "Control Flow"),
        new While("While", "Repeats based on condition.", "Control Flow"),
    };

    public Dictionary<string, Component> PaintItems { get; } = new();

    public Dictionary<string, Connection> Connections { get; set; } = new();
    private bool _isConnecting;
    private string _connectingConnectionId = "";

    public FlowchartSimulationView()
    {
        InitializeComponent();
        DataContext = this;

        Loaded += FlowchartSimulationView_Loaded;
        Unloaded += FlowchartSimulationView_Unloaded;
        PreviewMouseRightButtonDown += FlowchartSimulationView_PreviewMouseRightButtonDown;
        PreviewKeyDown += FlowchartSimulationView_PreviewKeyDown;
        Focusable = true;
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

        el.PaintSurface -= PalettePreview_OnPaintSurface;
        el.PaintSurface += PalettePreview_OnPaintSurface;
        el.InvalidateVisual();
    }

    private void FlowchartSimulationView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isConnecting)
            return;

        var conn = Connections.GetValueOrDefault(_connectingConnectionId);
        if (conn is null)
            return;

        PaintItems[conn.FromComponentId].Outputs[conn.FromId].OutputConnectionIds.Remove(_connectingConnectionId);
        Connections.Remove(_connectingConnectionId);
        _isConnecting = false;
        _connectingConnectionId = "";
        Debug.WriteLine("Cancelled Connection");
        skiaElement.InvalidateVisual();
    }

    private void FlowchartSimulationView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete)
            return;

        List<string> toRemove = new();

        foreach (var conn in Connections)
        {
            if (!conn.Value.IsSelected)
                continue;

            if (PaintItems[conn.Value.ToComponentId].IsInsideIf != "")
            {
                toRemove.Add(conn.Value.ToComponentId);
                ClearNestedConnectionScopes(PaintItems[conn.Value.ToComponentId].IsInsideIf, toRemove);
            }
            if (PaintItems[conn.Value.ToComponentId].IsInsideWhile != "")
            {
                toRemove.Add(conn.Value.ToComponentId);
                ClearNestedConnectionScopes(PaintItems[conn.Value.ToComponentId].IsInsideWhile, toRemove);
            }

            PaintItems[conn.Value.FromComponentId].Outputs[conn.Value.FromId].OutputConnectionIds.Remove(conn.Key);
            PaintItems[conn.Value.ToComponentId].Inputs[conn.Value.ToId].InputConnectionIds.Remove(conn.Key);
            Connections.Remove(conn.Key);
            Debug.WriteLine("Deleted Connection");
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        foreach (var item in PaintItems)
        {
            if (!item.Value.Selected)
                continue;

            foreach (var input in item.Value.Inputs.Values)
            {
                foreach (var id in input.InputConnectionIds.ToArray())
                {
                    if (!Connections.TryGetValue(id, out var conn))
                        continue;

                    PaintItems[conn.FromComponentId].Outputs[conn.FromId].OutputConnectionIds.Remove(id);
                    Connections.Remove(id);
                    Debug.WriteLine("Deleted Connection");
                }
                input.InputConnectionIds.Clear();
            }

            foreach (var output in item.Value.Outputs.Values)
            {
                foreach (var id in output.OutputConnectionIds.ToArray())
                {
                    if (!Connections.TryGetValue(id, out var conn))
                        continue;

                    toRemove.Add(conn.ToComponentId);
                    PaintItems[conn.ToComponentId].Inputs[conn.ToId].InputConnectionIds.Remove(id);
                    Connections.Remove(id);
                    Debug.WriteLine("Deleted Connection");
                }
                output.OutputConnectionIds.Clear();
            }

            ClearNestedConnectionScopes(item.Value.IsInsideIf, toRemove);
            ClearNestedConnectionScopes(item.Value.IsInsideWhile, toRemove);
            PaintItems.Remove(item.Key);
            Debug.WriteLine("Deleted Component");
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        e.Handled = true;
    }

    public void ClearNestedConnectionScopes(string scopeId, List<string> toVisit)
    {
        for (; toVisit.Count > 0;)
        {
            string current = toVisit.First();
            toVisit.RemoveAt(0);
            if (PaintItems[current].IsInsideIf == scopeId)
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

            if (PaintItems[current].IsInsideWhile == scopeId)
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
        }
    }

    private bool WouldCreateSkipConnection(string fromComponentId, string toComponentId)
    {
        if (string.IsNullOrWhiteSpace(fromComponentId) || string.IsNullOrWhiteSpace(toComponentId))
            return false;

        if (fromComponentId == toComponentId)
            return true;

        var visited = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<string>();
        stack.Push(fromComponentId);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!visited.Add(current))
                continue;

            foreach (var conn in Connections.Values)
            {
                if (string.IsNullOrWhiteSpace(conn.ToComponentId))
                    continue;

                if (!string.Equals(conn.FromComponentId, current, StringComparison.Ordinal))
                    continue;

                var next = conn.ToComponentId;
                if (string.Equals(next, toComponentId, StringComparison.Ordinal))
                    return true;

                stack.Push(next);
            }
        }

        return false;
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.WhiteSmoke);

        canvas.Scale(_zoom);
        canvas.Translate(_panOffset);

        foreach (var connection in Connections.Values)
        {
            var fromComponent = PaintItems[connection.FromComponentId];
            var fromNode = fromComponent.Outputs[connection.FromId].Node;

            var toPoint = connection.GetId() == _connectingConnectionId
                ? _mouseWorld
                : PaintItems[connection.ToComponentId].Inputs[connection.ToId].Node;

            canvas.DrawLine(fromNode, toPoint, connection.IsSelected ? Paints.SelectedLineStroke : Paints.LineStroke);
        }

        foreach (var item in PaintItems)
            item.Value.Paint(canvas);
    }


    private SKPoint ScreenToWorld(SKPoint screen) => new(screen.X / _zoom - _panOffset.X, screen.Y / _zoom - _panOffset.Y);

    private void PalettePreview_OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(new SKColor(0xF2, 0xF2, 0xF2));

        if (sender is not SKElement element || element.Tag is not Component c)
            return;

        DrawPalettePreview(canvas, e.Info.Width, e.Info.Height, c);
    }

    private static void DrawPalettePreview(SKCanvas canvas, int width, int height, Component c)
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

        var clone = Creator.Create(c.Name, c.Description, c.Category, 0, 0);

        var content = SKRect.Create(pad, pad, width - (pad * 2), height - (pad * 2));
        canvas.Translate(content.MidX, content.MidY);
        canvas.Scale(PalettePreviewZoom);

        clone.Paint(canvas);
        canvas.Restore();

        canvas.DrawRoundRect(clip, border);
    }

    public void CancelConnection()
    {
        if (Connections.TryGetValue(_connectingConnectionId, out var prev))
            PaintItems[prev.FromComponentId].Outputs[prev.FromId].OutputConnectionIds.Remove(_connectingConnectionId);
        Connections.Remove(_connectingConnectionId);
        _isConnecting = false;
        _connectingConnectionId = "";
    }

    private (string, Component, IO)? HitTest(SKPoint world)
    {
        (string, Component, IO)? hit = null;
        (string, Component, IO)? candidate = null;
        foreach (Component item in PaintItems.Values)
        {
            item.Selected = false;
            candidate = item.HitTest(world);
            if (candidate != null)
            {
                if (candidate.Value.Item1 == "output")
                {
                    if (!_isConnecting)
                    {
                        _isConnecting = true;
                        Connection newConnection = new Connection(candidate.Value.Item3.GetId(), "", candidate.Value.Item2.GetId(), "");
                        _connectingConnectionId = newConnection.GetId();
                        Connections.Add(_connectingConnectionId, newConnection);

                        item.Outputs[candidate.Value.Item3.GetId()].OutputConnectionIds.Add(_connectingConnectionId);
                    }
                    else
                    {
                        if (Connections.TryGetValue(_connectingConnectionId, out var prev))
                            PaintItems[prev.FromComponentId].Outputs[prev.FromId].OutputConnectionIds.Remove(_connectingConnectionId);

                        Connections.Remove(_connectingConnectionId);
                        _isConnecting = false;
                        _connectingConnectionId = "";
                    }
                    hit = candidate;
                }
                else if (candidate.Value.Item1 == "input")
                {
                    try
                    {
                        bool clearId = false;

                        if (_isConnecting && Connections.TryGetValue(_connectingConnectionId, out var inProgress))
                        {
                            var fromComponentId = inProgress.FromComponentId;
                            var toComponentId = candidate.Value.Item2.GetId();

                            if (WouldCreateSkipConnection(fromComponentId, toComponentId))
                            {
                                CancelConnection();
                                hit = candidate;
                                return hit;
                            }
                        }

                        if (item is While && item.Inputs.First().Value.GetId() == candidate.Value.Item3.GetId())
                        {
                            if (!PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile.Contains(item.GetId()) ||
                                candidate.Value.Item3.InputConnectionIds.Count > 0 ||
                                PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromId].OutputConnectionIds.Count > 1)
                            {
                                CancelConnection();
                                hit = candidate;
                                return hit;
                            }
                        }
                        else if (item.IsInsideIf != "" &&
                                 PaintItems[Connections[_connectingConnectionId].FromComponentId] is If &&
                                 PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromId].OutputConnectionIds.Count == 1 &&
                                 candidate.Value.Item3.InputConnectionIds.Count == 1)
                        {
                            clearId = true;
                        }
                        else if (item.IsInsideIf != "" &&
                                 item.IsInsideIf.Contains(PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf.Split("_")[0]) &&
                                 PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf.Split("_")[1] != item.IsInsideIf.Split("_")[1] &&
                                 PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromId].OutputConnectionIds.Count == 1)
                        {
                            Debug.WriteLine("Protected");
                            clearId = true;
                        }
                        else if (item.IsInsideIf != "" || item.IsInsideWhile != "")
                        {
                            CancelConnection();
                            hit = candidate;
                            return hit;
                        }
                        else if ((PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf != "" || PaintItems[Connections[_connectingConnectionId].FromComponentId] is If) &&
                                 PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromId].OutputConnectionIds.Count > 1)
                        {
                            CancelConnection();
                            hit = candidate;
                            return hit;
                        }
                        else if ((PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile != "" || PaintItems[Connections[_connectingConnectionId].FromComponentId] is While) &&
                                 PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromId].OutputConnectionIds.Count > 1)
                        {
                            CancelConnection();
                            hit = candidate;
                            return hit;
                        }
                        else if ((item.IsInsideIf != "" || item.IsInsideWhile != "") && item.Inputs[Connections[_connectingConnectionId].ToId].InputConnectionIds.Count > 0)
                        {
                            CancelConnection();
                            hit = candidate;
                            return hit;
                        }

                        if (_isConnecting && Connections[_connectingConnectionId].FromComponentId != candidate.Value.Item2.GetId())
                        {
                            if (PaintItems[Connections[_connectingConnectionId].FromComponentId] is If && !clearId)
                            {
                                item.IsInsideIf = Connections[_connectingConnectionId].FromComponentId + "_" + PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromId].IfTrue;
                            }
                            else if (PaintItems[Connections[_connectingConnectionId].FromComponentId] is While && PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromId].IfTrue != "False")
                            {
                                item.IsInsideWhile = Connections[_connectingConnectionId].FromComponentId + "_" + PaintItems[Connections[_connectingConnectionId].FromComponentId].Outputs[Connections[_connectingConnectionId].FromId].IfTrue;
                            }
                            else
                            {
                                if (clearId)
                                {
                                    if (PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf.Split("_")[0] != "")
                                    {
                                        item.IsInsideIf = PaintItems[PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf.Split("_")[0]].IsInsideIf;
                                    }
                                    else if (PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile.Split("_")[0] != "")
                                    {
                                        item.IsInsideWhile = PaintItems[PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile.Split("_")[0]].IsInsideWhile;
                                    }
                                }
                                else if (!(item is While && PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile.Contains(item.GetId())))
                                {
                                    item.IsInsideIf = PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideIf;
                                    item.IsInsideWhile = PaintItems[Connections[_connectingConnectionId].FromComponentId].IsInsideWhile;
                                }
                            }
                            Connections[_connectingConnectionId].ToId = candidate.Value.Item3.GetId();
                            Connections[_connectingConnectionId].ToComponentId = candidate.Value.Item2.GetId();
                            item.Inputs[candidate.Value.Item3.GetId()].InputConnectionIds.Add(_connectingConnectionId);
                            Connections[_connectingConnectionId].IsSelected = false;
                            _isConnecting = false;
                            _connectingConnectionId = "";
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

                    hit = candidate;
                }
                else if (candidate.Value.Item1 == "rect")
                {
                    item.Selected = true;
                    hit = candidate;
                    if (_isConnecting)
                    {
                        if (Connections.TryGetValue(_connectingConnectionId, out var prev))
                            PaintItems[prev.FromComponentId].Outputs[prev.FromId].OutputConnectionIds.Remove(_connectingConnectionId);

                        Connections.Remove(_connectingConnectionId);
                        _isConnecting = false;
                        _connectingConnectionId = "";
                    }
                }
                else if (candidate.Value.Item1 == "button")
                {
                    hit = candidate;
                }
            }
        }
        return hit;
    }

    private void SkiaElement_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        skiaElement.Focus();
        var p = e.GetPosition(skiaElement);
        var mouseScreen = new SKPoint((float)p.X, (float)p.Y);
        var mouseWorld = ScreenToWorld(mouseScreen);
        (string, Component, IO)? hit = HitTest(mouseWorld);

        foreach (var conn in Connections)
        {
            if (conn.Value.ToId == "" || (hit != null))
            {
                conn.Value.IsSelected = false;
                continue;
            }
            var fromNode = PaintItems[conn.Value.FromComponentId].Outputs[conn.Value.FromId].Node;
            var toNode = PaintItems[conn.Value.ToComponentId].Inputs[conn.Value.ToId].Node;
            var isSelected = conn.Value.HitTest(mouseWorld, fromNode, toNode, 5);
            if (isSelected)
            {
                skiaElement.InvalidateVisual();
                e.Handled = true;
                _isPanning = false;
                return;
            }
        }

        if (hit != null && hit.Value.Item1 == "rect")
        {
            _isMoving = true;
            LayersListView.SelectedItem = null;
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (LayersListView.SelectedItem != null)
        {
            int index = LayersListView.SelectedIndex;
            var selected = Items[index];
            var newComponent = Creator.Create(selected.Name, selected.Description, selected.Category, (int)mouseWorld.X, (int)mouseWorld.Y);

            PaintItems.Add(newComponent.GetId(), newComponent);
            LayersListView.SelectedItem = null;
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (hit != null && hit.Value.Item1 == "button")
        {
            skiaElement.InvalidateVisual();
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
        var p = e.GetPosition(skiaElement);
        var mouseScreen = new SKPoint((float)p.X, (float)p.Y);
        _mouseWorld = ScreenToWorld(mouseScreen);

        if (_isPanning)
        {
            var mouse = new SKPoint((float)p.X, (float)p.Y);
            var deltaScreen = mouse - _panStartMouse;
            _panOffset = _panStartOffset + deltaScreen;
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_isMoving)
        {
            skiaElement.Cursor = Cursors.SizeAll;
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
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        e.Handled = true;
    }

    private void SkiaElement_OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        _isPanning = false;
        _isMoving = false;
        skiaElement.ReleaseMouseCapture();
        skiaElement.Cursor = Cursors.Arrow;

        e.Handled = true;
    }

    private void SkiaElement_OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var p = e.GetPosition(skiaElement);
        var mouseScreen = new SKPoint((float)p.X, (float)p.Y);

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

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        ValueRegistry.ClearAllRegistries();
        ConsoleOutput.Text = "------Console Output------";

        foreach (var item in PaintItems.Values)
            item.Reset();

        Parser.ParseFlowchartAsync(PaintItems, Connections, cancellationToken);

        skiaElement.InvalidateVisual();
    }

    public void NewDiagram()
    {
        ValueRegistry.ClearAllRegistries();
        ConsoleOutput.Text = "------Console Output------";

        foreach (var item in PaintItems.Values)
            item.Reset();

        PaintItems.Clear();
        Connections.Clear();
        _isConnecting = false;
        _connectingConnectionId = "";
        skiaElement.InvalidateVisual();
    }

    public void SaveDiagram()
    {
        if (_isConnecting)
        {
            var conn = Connections.GetValueOrDefault(_connectingConnectionId);
            if (conn is not null)
                PaintItems[conn.FromComponentId].Outputs[conn.FromId].OutputConnectionIds.Remove(_connectingConnectionId);

            Connections.Remove(_connectingConnectionId);
            _isConnecting = false;
            _connectingConnectionId = "";
            Debug.WriteLine("Cancelled Connection");
            skiaElement.InvalidateVisual();
        }

        Creator.Save(PaintItems, Connections, "Flowchart");
    }

	public void SaveCanvasAsPng()
	{
		if (_isConnecting)
		{
			var conn = Connections.GetValueOrDefault(_connectingConnectionId);
			if (conn is not null)
				PaintItems[conn.FromComponentId].Outputs[conn.FromId].OutputConnectionIds.Remove(_connectingConnectionId);

			Connections.Remove(_connectingConnectionId);
			_isConnecting = false;
			_connectingConnectionId = "";
			skiaElement.InvalidateVisual();
		}

		CanvasExport.SaveAsPng(PaintItems, Connections);
	}

    public void LoadDiagram()
    {
        if (_isConnecting)
        {
            var conn = Connections.GetValueOrDefault(_connectingConnectionId);
            if (conn is not null)
                PaintItems[conn.FromComponentId].Outputs[conn.FromId].OutputConnectionIds.Remove(_connectingConnectionId);

            Connections.Remove(_connectingConnectionId);
            _isConnecting = false;
            _connectingConnectionId = "";
            skiaElement.InvalidateVisual();
        }

        var items = Creator.Load("Flowchart");
        if (items.Item1.Count == 0)
            return;

        PaintItems.Clear();
        Connections.Clear();
        foreach (var item in items.Item1)
            PaintItems.Add(item.Key, item.Value);
        foreach (var conn in items.Item2)
            Connections.Add(conn.Key, conn.Value);

        skiaElement.InvalidateVisual();
    }
}
