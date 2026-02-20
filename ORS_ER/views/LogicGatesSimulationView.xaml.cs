using ORS_ER.components;
using ORS_ER.connections;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Collections.ObjectModel;
using ICollectionView = System.ComponentModel.ICollectionView;
using System.Diagnostics;
using System.IO;
using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ORS_ER.views;

public partial class LogicGatesSimulationView : UserControl
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

    public ObservableCollection<Component> Items { get; } = new()
    {
        new BinaryInput("Binary Input", "Binary input.", "Inputs"),
        new BinaryOutput("Binary Output", "Outputs value of the circuit.", "Outputs"),
        new Gate("AND Gate", "Outputs true if both inputs are true.", "Gates"),
        new Gate("OR Gate", "Outputs true if at least one input is true.", "Gates"),
        new Gate("NOT Gate", "Outputs the inverse of the input.", "Gates"),
        new Gate("XOR Gate", "Outputs true if exactly one input is true.", "Gates"),
        new Gate("NAND Gate", "Outputs false if both inputs are true.", "Gates"),
        new Gate("NOR Gate", "Outputs true if both inputs are false.", "Gates"),
        new Gate("XNOR Gate", "Outputs true if both inputs are the same.", "Gates"),
        new Adder("Half Adder", "Half addition.", "Adders"),
        new Adder("Full Adder", "Full addition.", "Adders")
    };

    public ICollectionView FilteredItems { get; }

    private string _paletteQuery = string.Empty;
    private string _paletteCategory = "All";

    public Dictionary<string, Component> PaintItems { get; } = new();

    public Dictionary<string, Connection> connections { get; set; } = new();
    public bool _isConnecting { get; set; }
    private string _isConnectingId = "";

    public LogicGatesSimulationView()
    {
        InitializeComponent();
        DataContext = this;

        FilteredItems = CollectionViewSource.GetDefaultView(Items);
        FilteredItems.Filter = PaletteFilter;

        PreviewMouseRightButtonDown += FlowchartSimulationView_PreviewMouseRightButtonDown;
        PreviewKeyDown += FlowchartSimulationView_PreviewKeyDown;
        Focusable = true;
    }

    private bool PaletteFilter(object obj)
    {
        if (obj is not Component c)
            return false;

        if (!string.Equals(_paletteCategory, "All", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(c.Category, _paletteCategory, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(_paletteQuery))
            return true;

        return (c.Name?.Contains(_paletteQuery, StringComparison.OrdinalIgnoreCase) ?? false)
            || (c.Description?.Contains(_paletteQuery, StringComparison.OrdinalIgnoreCase) ?? false)
            || (c.Category?.Contains(_paletteQuery, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    public void FocusCanvas()
    {
        skiaElement.Focus();
        Focus();
    }

    public void ClearPaletteSelection() => LayersListView.SelectedItem = null;

    public void Invalidate() => skiaElement.InvalidateVisual();

    private void FlowchartSimulationView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isConnecting)
            return;

        var conn = connections.GetValueOrDefault(_isConnectingId);
        if (conn is null)
            return;

        PaintItems[conn.fromComponentId].Outputs[conn.fromId].outputConnectionIds.Remove(_isConnectingId);
        connections.Remove(_isConnectingId);
        _isConnecting = false;
        _isConnectingId = "";
        Debug.WriteLine("Cancelled Connection");
        skiaElement.InvalidateVisual();
    }

    private void FlowchartSimulationView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete)
            return;

        List<string> toRemove = new();

        foreach (var conn in connections)
        {
            if (!conn.Value.selected)
                continue;

            PaintItems[conn.Value.fromComponentId].Outputs[conn.Value.fromId].outputConnectionIds.Remove(conn.Key);
            PaintItems[conn.Value.toComponentId].Inputs[conn.Value.toId].inputConnectionIds.Remove(conn.Key);
            connections.Remove(conn.Key);
            Debug.WriteLine("Deleted Connection");
            skiaElement.InvalidateVisual();
            e.Handled = true;
            Parser.ParseCircuitAsync(PaintItems, connections);
            Parser.ParseCircuitAsync(PaintItems, connections);
            return;
        }

        foreach (var item in PaintItems)
        {
            if (!item.Value.Selected)
                continue;

            foreach (var input in item.Value.Inputs.Values)
            {
                foreach (var id in input.inputConnectionIds.ToArray())
                {
                    if (!connections.TryGetValue(id, out var conn))
                        continue;

                    PaintItems[conn.fromComponentId].Outputs[conn.fromId].outputConnectionIds.Remove(id);
                    connections.Remove(id);
                    Debug.WriteLine("Deleted Connection");
                }
                input.inputConnectionIds.Clear();
            }

            foreach (var output in item.Value.Outputs.Values)
            {
                foreach (var id in output.outputConnectionIds.ToArray())
                {
                    if (!connections.TryGetValue(id, out var conn))
                        continue;

                    PaintItems[conn.toComponentId].Inputs[conn.toId].inputConnectionIds.Remove(id);
                    connections.Remove(id);
                    Debug.WriteLine("Deleted Connection");
                }
                output.outputConnectionIds.Clear();
            }

            PaintItems.Remove(item.Key);
            Debug.WriteLine("Deleted Component");
            Parser.ParseCircuitAsync(PaintItems, connections);
            Parser.ParseCircuitAsync(PaintItems, connections);
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }

        e.Handled = true;
    }

    private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.WhiteSmoke);

        canvas.Scale(_zoom);
        canvas.Translate(_panOffset);

        foreach (var connection in connections.Values)
        {
            var fromComponent = PaintItems[connection.fromComponentId];
            var fromNode = fromComponent.Outputs[connection.fromId].node;

            var toPoint = connection.GetId() == _isConnectingId
                ? _mouseWorld
                : PaintItems[connection.toComponentId].Inputs[connection.toId].node;

            canvas.DrawLine(fromNode, toPoint, connection.selected ? Paints.SelectedLineStroke : Paints.LineStroke);
        }

        foreach (var item in PaintItems)
            item.Value.Paint(canvas);
    }

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
        var isGate = c is Gate;
        var scheme = isGate ? ComponentPaintScheme.Gate : ComponentPaintScheme.Input;
        isGate = c is Adder;
        scheme = isGate ? ComponentPaintScheme.Operator : scheme;
        var paints = ComponentPaints.Create(scheme);

        using var stroke = paints.SelectedLineStroke;
        using var fill = paints.ComponentFill;

        float pad = 6;
        var rect = new SKRect(pad, pad, width - pad, height - pad);
        var rrect = new SKRoundRect(rect, 6, 6);
        canvas.DrawRoundRect(rrect, fill);
        canvas.DrawRoundRect(rrect, stroke);

        var cx = rect.MidX;
        var cy = rect.MidY;

        var type = c.GetType().Name;

        if (type.Contains("Gate", StringComparison.OrdinalIgnoreCase))
        {
            using var text = new SKPaint { IsAntialias = true, Color = paints.ButtonTextPaint.Color, TextSize = Math.Max(10, rect.Height * 0.22f) };
            var label = c.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "G";
            var bounds = new SKRect();
            text.MeasureText(label, ref bounds);
            canvas.DrawText(label, cx - bounds.MidX, cy - bounds.MidY, text);
        }
        else if(type.Contains("Adder", StringComparison.OrdinalIgnoreCase))
        {
            using var text = new SKPaint { IsAntialias = true, Color = paints.ButtonTextPaint.Color, TextSize = Math.Max(10, rect.Height * 0.22f) };
            var label = c.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "G";
            var bounds = new SKRect();
            text.MeasureText(label, ref bounds);
            canvas.DrawText(label, cx - bounds.MidX, cy - bounds.MidY, text);
        }
        else if (type.Contains("Input", StringComparison.OrdinalIgnoreCase))
        {
            using var text = new SKPaint { IsAntialias = true, Color = paints.TextPaint.Color, TextSize = Math.Max(10, rect.Height * 0.22f) };
            var label = "In";
            var bounds = new SKRect();
            text.MeasureText(label, ref bounds);
            canvas.DrawText(label, cx - bounds.MidX, cy - bounds.MidY, text);
        }
        else if (type.Contains("Output", StringComparison.OrdinalIgnoreCase))
        {
            using var text = new SKPaint { IsAntialias = true, Color = paints.TextPaint.Color, TextSize = Math.Max(10, rect.Height * 0.22f) };
            var label = "Out";
            var bounds = new SKRect();
            text.MeasureText(label, ref bounds);
            canvas.DrawText(label, cx - bounds.MidX, cy - bounds.MidY, text);
        }
        else
        {
            var fallback = SKRect.Create(rect.Left + rect.Width * 0.25f, rect.Top + rect.Height * 0.25f, rect.Width * 0.5f, rect.Height * 0.5f);
            canvas.DrawRect(fallback, stroke);
        }
    }

    private SKPoint ScreenToWorld(SKPoint screen) => new(screen.X / _zoom - _panOffset.X, screen.Y / _zoom - _panOffset.Y);

    public void CancelConnection()
    {
        if (connections.TryGetValue(_isConnectingId, out var prev))
            PaintItems[prev.fromComponentId].Outputs[prev.fromId].outputConnectionIds.Remove(_isConnectingId);
        connections.Remove(_isConnectingId);
        _isConnecting = false;
        _isConnectingId = "";
    }

    private (string, Component, IO)? HitTest(SKPoint world)
    {
        (string, Component, IO)? returnItem = null;
        (string, Component, IO)? tmp = null;
        foreach (Component item in PaintItems.Values)
        {
            item.Selected = false;
            tmp = item.HitTest(world);
            if (tmp != null)
            {
                if (tmp.Value.Item1 == "output")
                {
                    if (!_isConnecting)
                    {
                        _isConnecting = true;
                        Connection newConnection = new Connection(tmp.Value.Item3.GetId(), "", tmp.Value.Item2.GetId(), "");
                        _isConnectingId = newConnection.GetId();
                        connections.Add(_isConnectingId, newConnection);

                        item.Outputs[tmp.Value.Item3.GetId()].outputConnectionIds.Add(_isConnectingId);
                    }
                    else
                    {
                        if (connections.TryGetValue(_isConnectingId, out var prev))
                            PaintItems[prev.fromComponentId].Outputs[prev.fromId].outputConnectionIds.Remove(_isConnectingId);

                        connections.Remove(_isConnectingId);
                        _isConnecting = false;
                        _isConnectingId = "";
                    }
                    returnItem = tmp;
                }
                else if (tmp.Value.Item1 == "input")
                {
                    try
                    {
                        if (_isConnecting && connections[_isConnectingId].fromComponentId != tmp.Value.Item2.GetId() && tmp.Value.Item3.inputConnectionIds.Count() == 0)
                        {
                            connections[_isConnectingId].toId = tmp.Value.Item3.GetId();
                            connections[_isConnectingId].toComponentId = tmp.Value.Item2.GetId();
                            item.Inputs[tmp.Value.Item3.GetId()].inputConnectionIds.Add(_isConnectingId);
                            connections[_isConnectingId].selected = false;
                            _isConnecting = false;
                            _isConnectingId = "";
                            Parser.ParseCircuitAsync(PaintItems, connections);
                            Parser.ParseCircuitAsync(PaintItems, connections);
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

                    returnItem = tmp;
                }
                else if (tmp.Value.Item1 == "rect")
                {
                    item.Selected = true;
                    returnItem = tmp;
                    if (_isConnecting)
                    {
                        if (connections.TryGetValue(_isConnectingId, out var prev))
                            PaintItems[prev.fromComponentId].Outputs[prev.fromId].outputConnectionIds.Remove(_isConnectingId);

                        connections.Remove(_isConnectingId);
                        _isConnecting = false;
                        _isConnectingId = "";
                    }
                }
                else if (tmp.Value.Item1 == "button")
                {
                    returnItem = tmp;
                }
            }
        }
        return returnItem;
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

        foreach (var conn in connections)
        {
            if (conn.Value.toId == "" || (hit != null))
            {
                conn.Value.selected = false;
                continue;
            }
            var fromNode = PaintItems[conn.Value.fromComponentId].Outputs[conn.Value.fromId].node;
            var toNode = PaintItems[conn.Value.toComponentId].Inputs[conn.Value.toId].node;
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
        else if (LayersListView.SelectedItem != null)
        {
            int index = LayersListView.SelectedIndex;
            var selected = Items[index];
            var newComponent = Creator.CreateLG(selected.Name, selected.Description, selected.Category, (int)mouseWorld.X, (int)mouseWorld.Y);

            PaintItems.Add(newComponent.GetId(), newComponent);
            LayersListView.SelectedItem = null;
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }
        else if (hit != null && hit.Value.Item1 == "button")
        {
            Parser.ParseCircuitAsync(PaintItems, connections);
            Parser.ParseCircuitAsync(PaintItems, connections);
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
        }
        else if (_isMoving)
        {
            skiaElement.Cursor = Cursors.SizeAll;
            foreach (var item in PaintItems)
            {
                if (item.Value.Selected)
                    item.Value.OffsetRect((int)_mouseWorld.X, (int)_mouseWorld.Y);
            }
            skiaElement.InvalidateVisual();
        }
        else if (_isConnecting)
        {
            skiaElement.InvalidateVisual();
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

    public void NewDiagram()
    {
        foreach (var item in PaintItems.Values)
            item.Reset();

        PaintItems.Clear();
        connections.Clear();
        _isConnecting = false;
        _isConnectingId = "";
        skiaElement.InvalidateVisual();
    }

    public void SaveDiagram()
    {
        if (_isConnecting)
        {
            var conn = connections.GetValueOrDefault(_isConnectingId);
            if (conn is not null)
                PaintItems[conn.fromComponentId].Outputs[conn.fromId].outputConnectionIds.Remove(_isConnectingId);

            connections.Remove(_isConnectingId);
            _isConnecting = false;
            _isConnectingId = "";
            skiaElement.InvalidateVisual();
        }

        Creator.Save(PaintItems, connections);
    }

	public void SaveCanvasAsPng()
	{
		if (_isConnecting)
		{
			var conn = connections.GetValueOrDefault(_isConnectingId);
			if (conn is not null)
				PaintItems[conn.fromComponentId].Outputs[conn.fromId].outputConnectionIds.Remove(_isConnectingId);

			connections.Remove(_isConnectingId);
			_isConnecting = false;
			_isConnectingId = "";
			skiaElement.InvalidateVisual();
		}

		CanvasExport.SaveAsPng(PaintItems, connections);
	}

    public void LoadDiagram()
    {
        var items = Creator.Load();
        if (items.Item1.Count == 0)
            return;

        PaintItems.Clear();
        connections.Clear();
        foreach (var item in items.Item1)
            PaintItems.Add(item.Key, item.Value);
        foreach (var conn in items.Item2)
            connections.Add(conn.Key, conn.Value);

        skiaElement.InvalidateVisual();
    }
}
