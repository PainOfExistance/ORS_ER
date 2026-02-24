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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ORS_ER.views;

public partial class LogicGatesSimulationView : UserControl
{
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


    public Dictionary<string, Component> PaintItems { get; } = new();

    public Dictionary<string, Connection> Connections { get; set; } = new();
    private bool _isConnecting;
    private string _connectingConnectionId = "";

    public LogicGatesSimulationView()
    {
        InitializeComponent();
        DataContext = this;

        foreach (var data in Creator.GetCachedLogicComponents())
            AddCustomComponentToPalette(data);

        PreviewMouseRightButtonDown += FlowchartSimulationView_PreviewMouseRightButtonDown;
        PreviewKeyDown += FlowchartSimulationView_PreviewKeyDown;
        Focusable = true;
    }

    public void SaveDiagramAsComponent(string? name = null, string? description = null, string? category = null)
    {
        var data = Creator.SaveLogicComponent(PaintItems, Connections, name, description, category);
        if (data is null)
            return;

        AddCustomComponentToPalette(data);
    }

    public void LoadLogicComponentFromFile()
    {
        var data = Creator.LoadLogicComponentFromFile();
        if (data is null)
            return;

        AddCustomComponentToPalette(data);
    }

    private void AddCustomComponentToPalette(Creator.SubCircuitData data)
    {
        if (Items.Any(item => string.Equals(item.Name, data.Name, StringComparison.OrdinalIgnoreCase)))
            return;

        Items.Add(new SubCircuitComponent(data));
    }

    public void FocusCanvas()
    {
        skiaElement.Focus();
        Focus();
    }

    public void ClearPaletteSelection() => LayersListView.SelectedItem = null;

    private void FlowchartSimulationView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        CancelPendingConnection(true, true);
    }

    private void FlowchartSimulationView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete)
            return;

        if (TryDeleteSelectedConnection())
        {
            CompleteDelete(e);
            return;
        }

        if (TryDeleteSelectedComponent())
        {
            CompleteDelete(e);
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

        foreach (var connection in Connections.Values)
        {
            var fromComponent = PaintItems[connection.FromComponentId];
            var fromNode = fromComponent.Outputs[connection.FromIOId].Node;

            var toPoint = connection.GetId() == _connectingConnectionId
                ? _mouseWorld
                : PaintItems[connection.ToComponentId].Inputs[connection.ToIOId].Node;

            canvas.DrawLine(fromNode, toPoint, connection.IsSelected ? Paints.SelectedLineStroke : Paints.LineStroke);
        }

        foreach (var item in PaintItems)
            item.Value.Paint(canvas);
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
        // so we get scheme of the component. Scheme is either Gate, Input or Operator.
        var isGate = component is Gate;
        var scheme = isGate ? ComponentPaintScheme.Gate : ComponentPaintScheme.Input;
        isGate = component is (Adder or SubCircuitComponent);
        scheme = isGate ? ComponentPaintScheme.Operator : scheme;

        // Original rects just smaller.
        var paints = ComponentPaints.Create(scheme);
        SKFont Font = new SKFont();

        using var stroke = paints.SelectedLineStroke;
        using var fill = paints.ComponentFill;

        float padding = 6;
        var rect = new SKRect(padding, padding, width - padding, height - padding);
        var roundedRect = new SKRoundRect(rect, 6, 6);
        canvas.DrawRoundRect(roundedRect, fill);
        canvas.DrawRoundRect(roundedRect, stroke);

        var centerX = rect.MidX;
        var centerY = rect.MidY + Font.Size / 4;

        while (rect.Width < (Font.MeasureText(component.Name) + 1))
        {
            Font.Size--;
        }

        centerX = rect.MidX - (Font.MeasureText(component.Name) / 2);
        canvas.DrawText(component.Name, centerX, centerY, Font, Paints.ButtonTextPaint);
    }

    private SKPoint ScreenToWorld(SKPoint screen) => new(screen.X / _zoom - _panOffset.X, screen.Y / _zoom - _panOffset.Y);

    public void CancelConnection()
    {
        CancelPendingConnection(false, false);
    }

    private void RunSimulation()
    {
        // Run twice to ensure propagated values stabilize across the circuit.
        Parser.RunCircuitSimulation(PaintItems, Connections);
        Parser.RunCircuitSimulation(PaintItems, Connections);
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

    private void RemoveInputConnections(IO input)
    {
        foreach (var id in input.InputConnectionIds.ToArray())
        {
            if (!Connections.TryGetValue(id, out var conn))
                continue;

            RemoveConnection(id, conn);
        }

        input.InputConnectionIds.Clear();
    }

    private void RemoveOutputConnections(IO output)
    {
        foreach (var id in output.OutputConnectionIds.ToArray())
        {
            if (!Connections.TryGetValue(id, out var conn))
                continue;

            RemoveConnection(id, conn);
        }

        output.OutputConnectionIds.Clear();
    }

    private void RemoveComponent(string componentId, Component component)
    {
        foreach (var input in component.Inputs.Values)
            RemoveInputConnections(input);

        foreach (var output in component.Outputs.Values)
            RemoveOutputConnections(output);

        PaintItems.Remove(componentId);
    }

    private bool TryDeleteSelectedConnection()
    {
        foreach (var connectionEntry in Connections.ToArray())
        {
            if (!connectionEntry.Value.IsSelected)
                continue;

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
        RunSimulation();
        skiaElement.InvalidateVisual();
        e.Handled = true;
    }

    private (HitTarget, Component, IO)? HitTest(SKPoint world)
    {
        (HitTarget, Component, IO)? hitResult = null;
        (HitTarget, Component, IO)? candidateResult = null;
        foreach (Component item in PaintItems.Values)
        {
            item.Selected = false;
            candidateResult = item.HitTest(world);
            if (candidateResult != null)
            {
                if (candidateResult.Value.Item1 == HitTarget.Output)
                {
                    // Hitting output starts connecting.
                    if (!_isConnecting)
                    {
                        _isConnecting = true;
                        Connection newConnection = new Connection(candidateResult.Value.Item3.GetId(), "", candidateResult.Value.Item2.GetId(), "");
                        _connectingConnectionId = newConnection.GetId();
                        Connections.Add(_connectingConnectionId, newConnection);

                        item.Outputs[candidateResult.Value.Item3.GetId()].OutputConnectionIds.Add(_connectingConnectionId);
                        hitResult = candidateResult;
                        return hitResult;
                    }
                    if (_isConnecting)
                    {
                        CancelPendingConnection(false, false);
                        hitResult = candidateResult;
                        return hitResult;
                    }
                }
                if (candidateResult.Value.Item1 == HitTarget.Input)
                {
                    try
                    {
                        // Hitting input causes connect if its connecting.
                        if (_isConnecting && Connections[_connectingConnectionId].FromComponentId != candidateResult.Value.Item2.GetId() && candidateResult.Value.Item3.InputConnectionIds.Count == 0)
                        {
                            Connections[_connectingConnectionId].ToIOId = candidateResult.Value.Item3.GetId();
                            Connections[_connectingConnectionId].ToComponentId = candidateResult.Value.Item2.GetId();
                            item.Inputs[candidateResult.Value.Item3.GetId()].InputConnectionIds.Add(_connectingConnectionId);
                            Connections[_connectingConnectionId].IsSelected = false;
                            _isConnecting = false;
                            _connectingConnectionId = "";
                            RunSimulation();
                        }
                        if (_isConnecting)
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
                    // Hitting rect selects the component.
                    item.Selected = true;
                    hitResult = candidateResult;
                    if (_isConnecting)
                    {
                        CancelPendingConnection(false, false);
                    }
                }
                if (candidateResult.Value.Item1 == HitTarget.Button)
                {
                    // Interaction/button funky stuff.
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
        if (e.ChangedButton != MouseButton.Left)
            return;

        skiaElement.Focus();
        var mousePosition = e.GetPosition(skiaElement);
        var mouseScreen = new SKPoint((float)mousePosition.X, (float)mousePosition.Y);
        var mouseWorld = ScreenToWorld(mouseScreen);
        (HitTarget, Component, IO)? hit = HitTest(mouseWorld);

        // Check if we hit connection or we deselect it.
        foreach (var connectionEntry in Connections)
        {
            if (connectionEntry.Value.ToIOId == "" || (hit != null))
            {
                connectionEntry.Value.IsSelected = false;
                continue;
            }
            var fromNode = PaintItems[connectionEntry.Value.FromComponentId].Outputs[connectionEntry.Value.FromIOId].Node;
            var toNode = PaintItems[connectionEntry.Value.ToComponentId].Inputs[connectionEntry.Value.ToIOId].Node;
            var isSelected = connectionEntry.Value.HitTest(mouseWorld, fromNode, toNode, 5);
            if (isSelected)
            {
                skiaElement.InvalidateVisual();
                e.Handled = true;
                _isPanning = false;
                return;
            }
        }

        if (hit != null && hit.Value.Item1 == HitTarget.Rect)
        {
            //Rect moving.
            _isMoving = true;
            LayersListView.SelectedItem = null;
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }
        if (LayersListView.SelectedItem != null)
        {
            // Adding item
            int selectedIndex = LayersListView.SelectedIndex;
            var selected = Items[selectedIndex];
            var newComponent = Creator.CreateLG(selected.Name, selected.Description, selected.Category, (int)mouseWorld.X, (int)mouseWorld.Y);

            PaintItems.Add(newComponent.GetId(), newComponent);
            LayersListView.SelectedItem = null;
            skiaElement.InvalidateVisual();
            e.Handled = true;
            return;
        }
        if (hit != null && hit.Value.Item1 == HitTarget.Button)
        {
            RunSimulation();
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
        var mousePosition = e.GetPosition(skiaElement);
        var mouseScreen = new SKPoint((float)mousePosition.X, (float)mousePosition.Y);
        _mouseWorld = ScreenToWorld(mouseScreen);

        if (_isPanning)
        {
            var mouse = new SKPoint((float)mousePosition.X, (float)mousePosition.Y);
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
        var mousePosition = e.GetPosition(skiaElement);
        var mouseScreen = new SKPoint((float)mousePosition.X, (float)mousePosition.Y);

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
        Connections.Clear();
        _isConnecting = false;
        _connectingConnectionId = "";
        skiaElement.InvalidateVisual();
    }

    public void SaveDiagram()
    {
        CancelPendingConnection(true, false);

        Creator.Save(PaintItems, Connections, "LogicGates");
    }

    public void SaveCanvasAsPng()
    {
        CancelPendingConnection(true, false);

        CanvasExport.SaveAsPng(PaintItems, Connections);
    }

    public void LoadDiagram()
    {
        var items = Creator.Load("LogicGates");
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
