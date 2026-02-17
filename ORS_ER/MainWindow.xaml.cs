using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ORS_ER.components;
using ORS_ER.connections;
using ORS_ER.views;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Threading;

namespace ORS_ER
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, UserControl> _simulationCache = new(StringComparer.Ordinal);

        private FlowchartSimulationView FlowchartView
        {
            get
            {
                if (_simulationCache.TryGetValue("Flowchart", out var existing))
                    return (FlowchartSimulationView)existing;

                var created = new FlowchartSimulationView();
                _simulationCache["Flowchart"] = created;
                return created;
            }
        }

        private UserControl LogicGatesView
        {
            get
            {
                if (_simulationCache.TryGetValue("Logic Gates", out var existing))
                    return existing;

                var created = new LogicGatesSimulationView();
                _simulationCache["Logic Gates"] = created;
                return created;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= MainWindow_Loaded;

            SimulationPicker.SelectionChanged += SimulationPicker_SelectionChanged;
            SimulationHost.Content = GetSelectedSimulation();

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
                Run.IsEnabled = true;
            }
            else if (SimulationHost.Content is LogicGatesSimulationView lg)
            {
                //lg.FocusCanvas();
                Run.IsEnabled = false;
            }
        }

        private async void Run_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is not FlowchartSimulationView fc)
                return;

            try
            {
                var cts = new CancellationTokenSource();
                await fc.RunAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error during code generation: " + ex.Message);
            }

            e.Handled = true;
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is FlowchartSimulationView fc)
                fc.NewDiagram();

                e.Handled = true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is FlowchartSimulationView fc)
                fc.SaveDiagram();

            e.Handled = true;
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedSimulation() is FlowchartSimulationView fc)
                fc.LoadDiagram();

            e.Handled = true;
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