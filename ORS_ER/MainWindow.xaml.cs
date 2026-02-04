using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ORS_ER.components;
using ORS_ER.connections;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CSharp;

namespace ORS_ER
{
    public partial class MainWindow : Window
    {
        private SKPoint _panOffset = new(0, 0);
        private float _zoom = 1.0f;
        private bool _isPanning = false;
        private bool _isMoving = false;
        private SKPoint _panStartMouse;
        private SKPoint _panStartOffset;
        private SKPoint _mouseWorld;
        private const float MinZoom = 0.1f;
        private const float MaxZoom = 10.0f;
        private const float ZoomStep = 1.1f;
        private static readonly ComponentPaints Paints = ComponentPaints.Create(ComponentPaintScheme.Input);

        public ObservableCollection<Component> Items { get; } = new()
        {
            new Input("String Input", "String input.", "Inputs", 0),
            new Input("Numerical Input", "Numerical input.", "Inputs", 0),
            new Input("Binary Input", "Outputs binary value.", "Inputs", 0),
            new Print("Print", "Prints to console.", "Outputs", 0),
            new BinaryPrint("Binary Print", "Prints binary value to console.", "Outputs", 0),
            new Logic("Logic Block", "Performs logical operation.", "Logic", 0),
        };

        public Dictionary<string, Component> PaintItems { get; } = new()
        {
        };

        public Dictionary<string, Connection> connections { get; set; } = new Dictionary<string, Connection>();
        public bool _isConnecting { get; set; } = false;
        string _isConnectingId = "";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            PreviewMouseRightButtonDown += MainWindow_PreviewMouseRightButtonDown;
            PreviewKeyDown += MainWindow_PreviewKeyDown;
            ConsoleOutput.Text = "------Console Output------\n";
            var uiWriter = new UiTextBlockWriter(ConsoleOutput);
            Console.SetOut(TextWriter.Synchronized(uiWriter));
            Console.SetError(TextWriter.Synchronized(uiWriter));

            Focusable = true;
            Focus();
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            if (source is null)
                return;

            var clickedListViewItem = FindAncestor<ListViewItem>(source);
            if (clickedListViewItem is null && source.GetType().FullName != "SkiaSharp.Views.WPF.SKElement")
                LayersListView.SelectedItem = null;
        }

        private void MainWindow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isConnecting)
                return;
            var conn = connections.GetValueOrDefault(_isConnectingId);
            PaintItems[conn.fromComponentId].Outputs[conn.fromId].outputConnectionId = "";
            connections.Remove(_isConnectingId);
            _isConnecting = false;
            _isConnectingId = "";
            Debug.WriteLine("Cancelled Connection");
            skiaElement.InvalidateVisual();
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                foreach (var conn in connections)
                {
                    if (conn.Value.selected)
                    {
                        PaintItems[conn.Value.fromComponentId].Outputs[conn.Value.fromId].outputConnectionId = "";
                        PaintItems[conn.Value.toComponentId].Inputs[conn.Value.toId].inputConnectionId = "";
                        connections.Remove(conn.Key);
                        Debug.WriteLine("Deleted Connection");
                        skiaElement.InvalidateVisual();
                        break;
                    }
                }

                foreach (var item in PaintItems)
                {
                    if (item.Value.Selected)
                    {
                        foreach (var input in item.Value.Inputs.Values)
                        {
                            if (input.inputConnectionId != "")
                            {
                                var conn = connections[input.inputConnectionId];
                                PaintItems[conn.fromComponentId].Outputs[conn.fromId].outputConnectionId = "";
                                connections.Remove(input.inputConnectionId);
                                Debug.WriteLine("Deleted Connection");
                            }
                        }
                        foreach (var output in item.Value.Outputs.Values)
                        {
                            if (output.outputConnectionId != "")
                            {
                                var conn = connections[output.outputConnectionId];
                                PaintItems[conn.toComponentId].Inputs[conn.toId].inputConnectionId = "";
                                connections.Remove(output.outputConnectionId);
                                Debug.WriteLine("Deleted Connection");
                            }
                        }
                        PaintItems.Remove(item.Key);
                        Debug.WriteLine("Deleted Component");
                        skiaElement.InvalidateVisual();
                        break;
                    }
                }
            }

            e.Handled = true;
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current is not null)
            {
                if (current is T match)
                    return match;

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

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
            {
                item.Value.Paint(canvas);
            }
        }

        private SKPoint ScreenToWorld(SKPoint screen) => new(screen.X / _zoom - _panOffset.X, screen.Y / _zoom - _panOffset.Y);

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
                            item.Outputs[tmp.Value.Item3.GetId()].outputConnectionId = _isConnectingId;
                        }
                        else
                        {
                            connections.Remove(_isConnectingId);
                            _isConnecting = false;
                            _isConnectingId = "";
                        }
                        returnItem = tmp;
                    }
                    else if (tmp.Value.Item1 == "input")
                    {
                        if (_isConnecting && tmp.Value.Item3.inputConnectionId == "" && connections[_isConnectingId].fromComponentId != tmp.Value.Item2.GetId())
                        {
                            connections[_isConnectingId].toId = tmp.Value.Item3.GetId();
                            connections[_isConnectingId].toComponentId = tmp.Value.Item2.GetId();
                            item.Inputs[tmp.Value.Item3.GetId()].inputConnectionId = _isConnectingId;
                            connections[_isConnectingId].selected = false;
                            _isConnecting = false;
                            _isConnectingId = "";
                        }
                        else
                        {
                            connections.Remove(_isConnectingId);
                            _isConnecting = false;
                            _isConnectingId = "";
                        }
                        returnItem = tmp;
                    }
                    else if (tmp.Value.Item1 == "rect")
                    {
                        item.Selected = true;
                        returnItem = tmp;
                        if (_isConnecting)
                        {
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
                var newComponent = Creator.Create(selected.Name, selected.Description, selected.Category, (int)mouseWorld.X, (int)mouseWorld.Y);

                PaintItems.Add(newComponent.GetId(), newComponent);
                LayersListView.SelectedItem = null;
                skiaElement.InvalidateVisual();
                e.Handled = true;
                return;
            }
            else if (hit != null && hit.Value.Item1 == "button")
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
            }
            else if (_isMoving)
            {
                skiaElement.Cursor = Cursors.SizeAll;
                foreach (var item in PaintItems)
                {
                    if (item.Value.Selected)
                    {
                        item.Value.OffsetRect((int)_mouseWorld.X, (int)_mouseWorld.Y);
                    }
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

        private async void Run_Click(object sender, RoutedEventArgs e)
        {
            var cts = new CancellationTokenSource();
            string code = await Parser.ParseAsync(PaintItems, connections, cts.Token);
            Debug.WriteLine("Generated Code:");
            Debug.WriteLine(code);

            ConsoleOutput.Text = "------Console Output------\n";
            try
            {
                await Parser.EvaluateAsync(code, cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            skiaElement.InvalidateVisual();
            e.Handled = true;
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            PaintItems.Clear();
            connections.Clear();
            _isConnecting = false;
            _isConnectingId = "";
            skiaElement.InvalidateVisual();
            e.Handled = true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnecting)
            {
                var conn = connections.GetValueOrDefault(_isConnectingId);
                PaintItems[conn.fromComponentId].Outputs[conn.fromId].outputConnectionId = "";
                connections.Remove(_isConnectingId);
                _isConnecting = false;
                _isConnectingId = "";
                Debug.WriteLine("Cancelled Connection");
                skiaElement.InvalidateVisual();
            }

            Creator.Save(PaintItems, connections);
            e.Handled = true;
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var items = Creator.Load();
            if (items.Item1.Count == 0)
            {
                return;
            }
            PaintItems.Clear();
            connections.Clear();
            foreach (var item in items.Item1)
            {
                PaintItems.Add(item.Key, item.Value);
            }
            foreach (var conn in items.Item2)
            {
                connections.Add(conn.Key, conn.Value);
            }
            e.Handled = true;
            skiaElement.InvalidateVisual();
        }
    }
}
/*
•	Selection box + multi-select (Shift/Ctrl) and group move.
•	Copy/Paste/Duplicate of components and subgraphs.
•	Connection validation (type compatibility, cycle prevention).
•	Zoom-to-fit and reset view actions.
•	Export canvas to PNG/SVG.
•	Inline error highlights on invalid connections or runtime errors.
*/