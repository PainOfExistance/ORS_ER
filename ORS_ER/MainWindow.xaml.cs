using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ORS_ER.components;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace ORS_ER
{
    public partial class MainWindow : Window
    {
        private SKPoint _panOffset = new(0, 0);
        private float _zoom = 1.0f;

        private bool _isPanning;
        private bool _isMoving;
        private SKPoint _panStartMouse;
        private SKPoint _panStartOffset;

        private const float MinZoom = 0.1f;
        private const float MaxZoom = 10.0f;
        private const float ZoomStep = 1.1f;

        public ObservableCollection<Component> Items { get; } = new()
        {
            new components.Input("Input", "Any type input.", "Inputs"),
            new components.Print("Print", "Prints to console.", "Outputs"),
        };

        public ObservableCollection<Component> PaintItems { get; } = new()
        {
        };

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
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

            foreach (var item in PaintItems)
            {
                item.Paint(canvas);
            }
        }

        private SKPoint ScreenToWorld(SKPoint screen) => new(screen.X / _zoom - _panOffset.X, screen.Y / _zoom - _panOffset.Y);

        private Component? HitTest(SKPoint world)
        {
            Component ReturnItem = null;
            foreach (Component item in PaintItems)
            {
                item.Selected = false;
                if (item.Rect.Contains(world))
                {
                    item.Selected = true;
                    ReturnItem = item;
                }
            }

            return ReturnItem;
        }

        private void SkiaElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            var p = e.GetPosition(skiaElement);
            var mouseScreen = new SKPoint((float)p.X, (float)p.Y);
            var mouseWorld = ScreenToWorld(mouseScreen);
            var hit = HitTest(mouseWorld);

            if (hit != null)
            {
                _isMoving = true;
                LayersListView.SelectedItem = null;
                e.Handled = true;
                return;
            }
            else if (LayersListView.SelectedItem != null)
            {
                int index = LayersListView.SelectedIndex;
                var selected = Items[index];
                var type = selected.GetType();
                if (Activator.CreateInstance(type, selected.Name, selected.Description, selected.Category) is Component newComponent)
                {
                    PaintItems.Add(newComponent);
                    int paintIndex = PaintItems.Count - 1;
                    PaintItems[paintIndex].Selected = true;
                    PaintItems[paintIndex].CreateRect((int)mouseWorld.X, (int)mouseWorld.Y);
                }
                LayersListView.SelectedItem = null;
                skiaElement.InvalidateVisual();
            }

            _isPanning = true;
            skiaElement.CaptureMouse();
            skiaElement.Cursor = Cursors.SizeAll;

            _panStartMouse = mouseScreen;
            _panStartOffset = _panOffset;

            e.Handled = true;
        }

        private void SkiaElement_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning)
            {
                var p = e.GetPosition(skiaElement);
                var mouse = new SKPoint((float)p.X, (float)p.Y);

                var deltaScreen = mouse - _panStartMouse;
                _panOffset = _panStartOffset + deltaScreen;
            }
            else if (_isMoving)
            {
                var p = e.GetPosition(skiaElement);
                var mouseScreen = new SKPoint((float)p.X, (float)p.Y);
                var mouseWorld = ScreenToWorld(mouseScreen);
                skiaElement.Cursor = Cursors.SizeAll;
                foreach (var item in PaintItems)
                {
                    if (item.Selected)
                    {
                        item.CreateRect((int)mouseWorld.X, (int)mouseWorld.Y);
                    }
                }
            }

            skiaElement.InvalidateVisual();
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
    }
}