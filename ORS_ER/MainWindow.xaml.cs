using ORS_ER.components;
using ORS_ER.connections;
using ORS_ER.views;
using ORS_ER.windows;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ORS_ER
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, UserControl> _simulationCache = new(StringComparer.Ordinal);

        private FlowchartSimulationView FlowchartView
            => (FlowchartSimulationView)GetOrCreateSimulation("Flowchart", static () => new FlowchartSimulationView());

        private LogicGatesSimulationView LogicGatesView
            => (LogicGatesSimulationView)GetOrCreateSimulation("Logic Gates", static () => new LogicGatesSimulationView());

        private UserControl GetOrCreateSimulation(string key, Func<UserControl> factory)
        {
            if (_simulationCache.TryGetValue(key, out var existing))
                return existing;

            var created = factory();
            _simulationCache[key] = created;
            return created;
        }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            StatusLabel.Content = "No file loaded";
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;

            SimulationPicker.SelectionChanged += SimulationPicker_SelectionChanged;
            SimulationHost.Content = GetSelectedSimulation();

            SaveComponent.IsEnabled = SimulationHost.Content is LogicGatesSimulationView;
            LoadComponent.IsEnabled = SimulationHost.Content is LogicGatesSimulationView;

            if (SimulationHost.Content is FlowchartSimulationView fc)
                fc.FocusCanvas();
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            if (source is null)
                return;

            var clickedListViewItem = FindAncestor<ListViewItem>(source);
            if (clickedListViewItem is null && source.GetType().FullName != "SkiaSharp.Views.WPF.SKElement")
            {
                if (SimulationHost.Content is FlowchartSimulationView fc)
                    fc.ClearPaletteSelection();
            }
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

        private string GetSelectedSimulationName()
            => (SimulationPicker.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Flowchart";

        private UserControl GetSelectedSimulation()
            => GetSelectedSimulationName() switch
            {
                "Logic Gates" => LogicGatesView,
                _ => FlowchartView,
            };

        private void SimulationPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SimulationHost is null || Run is null)
                return;

            SimulationHost.Content = GetSelectedSimulation();

            if (SimulationHost.Content is FlowchartSimulationView fc)
            {
                fc.FocusCanvas();
                fc.LoadedFilePath = string.Empty;
                StatusLabel.Content = "No file loaded";
                Save.IsEnabled = false;
                Run.IsEnabled = true;
                SaveComponent.IsEnabled = false;
                LoadComponent.IsEnabled = false;
                fc.PaintItems.Clear();
                fc.Connections.Clear();
                fc.ConsoleOutput.Clear();
                fc.ConsoleOutput.Text = "------Console Output------";
                return;
            }

            if (SimulationHost.Content is LogicGatesSimulationView lg)
            {
                lg.FocusCanvas();
                lg.LoadedFilePath = string.Empty;
                StatusLabel.Content = "No file loaded";
                Save.IsEnabled = false;
                Run.IsEnabled = false;
                SaveComponent.IsEnabled = true;
                LoadComponent.IsEnabled = true;
                lg.PaintItems.Clear();
                lg.Connections.Clear();
            }
        }

        private async void Run_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is not FlowchartSimulationView fc)
                return;

            try
            {
                await fc.RunAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error during code generation: " + ex.Message);
            }

            e.Handled = true;
        }

        private void SaveComponent_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is LogicGatesSimulationView lg)
                lg.SaveDiagramAsComponent();

            e.Handled = true;
        }

        private void LoadComponent_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is LogicGatesSimulationView lg)
                lg.LoadLogicComponentFromFile();

            e.Handled = true;
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is FlowchartSimulationView fc)
            {
                fc.NewDiagram();
                fc.LoadedFilePath = string.Empty;
                StatusLabel.Content = "No file loaded";
                Save.IsEnabled = true;
                e.Handled = true;
                return;
            }

            if (GetSelectedSimulation() is LogicGatesSimulationView lg)
            {
                lg.NewDiagram();
                lg.LoadedFilePath = string.Empty;
                StatusLabel.Content = "No file loaded";
                Save.IsEnabled = true;
                e.Handled = true;
                return;
            }

            e.Handled = true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is FlowchartSimulationView fc)
            {
                fc.SaveDiagram();
                e.Handled = true;
                return;
            }

            if (GetSelectedSimulation() is LogicGatesSimulationView lg)
            {
                lg.SaveDiagram();
                e.Handled = true;
                return;
            }

            e.Handled = true;
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is FlowchartSimulationView fc)
            {
                var meow = GetSelectedSimulation();
                fc.SaveDiagramAs();
                StatusLabel.Content = string.IsNullOrEmpty(fc.LoadedFilePath) ? "No file loaded" : $"Loaded: {System.IO.Path.GetFileName(fc.LoadedFilePath)}";
                Save.IsEnabled = true;
                e.Handled = true;
                return;
            }

            if (GetSelectedSimulation() is LogicGatesSimulationView lg)
            {
                lg.SaveDiagramAs();
                StatusLabel.Content = string.IsNullOrEmpty(lg.LoadedFilePath) ? "No file loaded" : $"Loaded: {System.IO.Path.GetFileName(lg.LoadedFilePath)}";
                Save.IsEnabled = true;
                e.Handled = true;
                return;
            }

            e.Handled = true;
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is FlowchartSimulationView fc)
            {
                bool success = fc.LoadDiagram();
                if (success)
                {
                    StatusLabel.Content = string.IsNullOrEmpty(fc.LoadedFilePath) ? "No file loaded" : $"Loaded: {System.IO.Path.GetFileName(fc.LoadedFilePath)}";
                    Save.IsEnabled = true;
                    e.Handled = true;
                }
                return;
            }

            if (GetSelectedSimulation() is LogicGatesSimulationView lg)
            {
                bool success = lg.LoadDiagram();
                if (success)
                {
                    StatusLabel.Content = string.IsNullOrEmpty(lg.LoadedFilePath) ? "No file loaded" : $"Loaded: {System.IO.Path.GetFileName(lg.LoadedFilePath)}";
                    Save.IsEnabled = true;
                    e.Handled = true;
                }
                return;
            }

            e.Handled = true;
        }

        private void ExportPng_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is FlowchartSimulationView fc)
            {
                fc.SaveCanvasAsPng();
                e.Handled = true;
                return;
            }

            if (GetSelectedSimulation() is LogicGatesSimulationView lg)
            {
                lg.SaveCanvasAsPng();
                e.Handled = true;
                return;
            }

            e.Handled = true;
        }

        private void StressZest_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is FlowchartSimulationView fc)
            {
                fc.StressTest();
                e.Handled = true;
                return;
            }

            e.Handled = true;
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow();
            helpWindow.Show();
            e.Handled = true;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (SimulationHost.Content is FlowchartSimulationView fc)
                {
                    fc.Delete(e);
                    e.Handled = true;
                    return;
                }

                if (SimulationHost.Content is LogicGatesSimulationView lg)
                {
                    lg.Delete(e);
                    e.Handled = true;
                    return;
                }
            }
            else if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && e.Key == Key.D)
            {
                if (SimulationHost.Content is FlowchartSimulationView fc)
                {
                    fc.Duplicate();
                    e.Handled = true;
                    return;
                }

                if (SimulationHost.Content is LogicGatesSimulationView lg)
                {
                    lg.Duplicate();
                    e.Handled = true;
                    return;
                }
            }
            else if(((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && e.Key == Key.C)
            {
                if (SimulationHost.Content is FlowchartSimulationView fc)
                {
                    fc.Copy();
                    e.Handled = true;
                    return;
                }

                if (SimulationHost.Content is LogicGatesSimulationView lg)
                {
                    lg.Copy();
                    e.Handled = true;
                    return;
                }
            }
            else if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && e.Key == Key.V)
            {
                if (SimulationHost.Content is FlowchartSimulationView fc)
                {
                    fc.Paste();
                    e.Handled = true;
                    return;
                }

                if (SimulationHost.Content is LogicGatesSimulationView lg)
                {
                    lg.Paste();
                    e.Handled = true;
                    return;
                }
            }
            else if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && e.Key == Key.Z)
            {
                if (SimulationHost.Content is FlowchartSimulationView fc)
                {
                    _ = fc.UndoAsync();
                    e.Handled = true;
                    return;
                }

                if (SimulationHost.Content is LogicGatesSimulationView lg)
                {
                    lg.Undo();
                    e.Handled = true;
                    return;
                }
            }
            else if (((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) && e.Key == Key.Y)
            {
                if (SimulationHost.Content is FlowchartSimulationView fc)
                {
                    _ = fc.RedoAsync();
                    e.Handled = true;
                    return;
                }

                if (SimulationHost.Content is LogicGatesSimulationView lg)
                {
                    lg.Redo();
                    e.Handled = true;
                    return;
                }
            }

            e.Handled = true;
            return;
        }
    }
}
