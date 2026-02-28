# ORS_ER

## Overview
ORS_ER is a WPF desktop app for building and simulating two kinds of diagrams: flowcharts and logic gate circuits. It uses SkiaSharp for rendering, supports saving/loading diagrams, and can export the canvas to PNG.

## Features
- Flowchart editor with drag-and-drop blocks: inputs, operators, conditionals, loops, and print/output blocks.
- Logic gates editor with binary inputs/outputs, gates, adders, and reusable subcircuits.
- Live simulation: flowchart execution writes to the in-app console; logic circuits propagate binary values through connections.
- Save and load diagrams as JSON.
- Export the canvas to PNG.
- Save logic gate diagrams as custom components and load them into the palette.

## Project Structure
- `ORS_ER/App.xaml` and `ORS_ER/MainWindow.xaml`: application startup and main UI shell.
- `ORS_ER/views/`:
  - `FlowchartSimulationView.xaml`: flowchart editor, execution, and console output handling.
  - `LogicGatesSimulationView.xaml`: logic circuit editor, simulation, and custom component palette.
- `ORS_ER/components/`:
  - `Component.cs`: base class for drawable nodes with IO ports.
  - Flowchart blocks: `Input`, `Operator`, `If`, `While`, `Print`.
  - Logic blocks: `Gate`, `BinaryInput`, `BinaryOutput`, `Adder`.
  - `SubCircuitComponent`: wraps saved logic diagrams as reusable components.
  - `ComponentPaintScheme`: shared rendering styles.
- `ORS_ER/connections/`:
  - `Connection` and `IO`: wiring metadata and port definitions.
  - `ValueRegistry`: variable/value scope tracking for flowchart execution.
- `ORS_ER/windows/`: dialogs used to configure inputs, operators, conditions, and print blocks.
- `ORS_ER/Creator.cs`: builds components, saves/loads diagrams, and handles subcircuit serialization.
- `ORS_ER/Parser.cs`: traverses diagrams to simulate logic or execute flowcharts.
- `ORS_ER/CanvasExport.cs`: exports a diagram to PNG.
- `ORS_ER/UiTextBlockWriter.cs`: streams console output into the UI.

## Class Details
### Application and Views
- `App`: WPF application entry point.
- `MainWindow`: hosts the simulation picker, commands (run/save/load/export), and caches the flowchart/logic views.
- `FlowchartSimulationView`: manages flowchart canvas interactions, connection rules, execution (`RunAsync`), and console output.
- `LogicGatesSimulationView`: manages logic circuit canvas interactions, simulation triggers, and custom component palette.

### Components
- `Component`: base drawable node with inputs/outputs, hit testing, serialization, and interaction dispatch.
- `Input`: flowchart input block for numeric/string/binary variables, registers values in `ValueRegistry`.
- `Operator`: flowchart operator block for arithmetic/boolean comparisons and assignments.
- `Print`: flowchart output block that writes values to the UI console.
- `If`: flowchart conditional block that evaluates a comparison and gates outgoing traversal.
- `While`: flowchart loop block that evaluates a comparison and controls loop traversal.
- `Gate`: logic gate block (AND, OR, NOT, etc.) that computes a boolean output.
- `BinaryInput`: logic input toggle for boolean signals.
- `BinaryOutput`: logic output indicator for boolean signals.
- `Adder`: half/full adder that outputs sum and carry bits.
- `SubCircuitComponent`: wraps saved logic diagrams as reusable components with internal simulation.
- `ComponentPaintScheme` and `ComponentPaints`: shared rendering style palette for components and connections.

### Connections and State
- `Connection`: link between component IO ports with hit testing and serialization helpers.
- `IO`: input/output port metadata, including connection lists and branch labels.
- `ValueRegistry`: scoped variable/value storage for flowchart execution.
- `RegistryId` and `RegistryKey`: scope and key value objects used by `ValueRegistry`.

### Dialog Windows
- `InputWindow`: configures input variable name and value.
- `LogicWindow`: configures operator expressions.
- `IfWindow`: configures `If`/`While` conditions.
- `PrintWindow`: selects the variable to print.

### Utilities
- `Creator`: creates components, saves/loads diagrams, and serializes custom logic components.
- `Parser`: traverses diagrams to execute flowcharts or simulate logic circuits.
- `CanvasExport`: renders diagrams to PNG.
- `UiTextBlockWriter`: redirects console output to the UI.

## Build and Run
- Requires .NET 10 and Windows.
- Build:
  - `dotnet build ORS_ER/ORS_ER.csproj`
- Run:
  - `dotnet run --project ORS_ER/ORS_ER.csproj`

## Usage
- Use the simulation picker to switch between Flowchart and Logic Gates.
- Drag blocks from the palette onto the canvas and connect them.
- Connect them by ckilicking an output port, then an input port. For flowcharts, connections represent execution flow; for logic gates, they represent signal flow.
- Flowchart: select `Run` to execute and view output in the console pane.
- Logic Gates: toggle binary inputs and interact with components to update outputs.
- Use the menu to save/load diagrams or export PNGs.
