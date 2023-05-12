using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Serialization;
using MFFSuCA.Enums;
using MFFSuCA.Models;
using MFFSuCA.Utility;
using Microsoft.Win32;

namespace MFFSuCA.Windows;

/// <summary>
/// Interaction logic for SimulationWindow.xaml
/// </summary>
public partial class SimulationWindow {
    /// <summary>
    /// Size of one rectangle in the map canvas.
    /// </summary>
    private const int RectSize = 10;

    /// <summary>
    /// List of rectangles in the map canvas.
    /// </summary>
    private List<List<Rectangle>> _rectArray = new();

    /// <summary>
    /// Dictionary, which maps brush types to brushes.
    /// </summary>
    private readonly Dictionary<BrushType, Brush> _brushes = new() {
        { BrushType.Forest, new SolidColorBrush(Colors.DarkGreen) },
        { BrushType.Grassland, new SolidColorBrush(Colors.Green) },
        { BrushType.Plain, new SolidColorBrush(Colors.PaleGreen) },
        { BrushType.Mountain, new SolidColorBrush(Colors.SaddleBrown) },
        { BrushType.Water, new SolidColorBrush(Colors.Blue) },
        { BrushType.HighDensityUrbanArea, new SolidColorBrush(Colors.DimGray) },
        { BrushType.LowDensityUrbanArea, new SolidColorBrush(Colors.DarkGray) },
        { BrushType.Clear, new SolidColorBrush(Colors.White) }
    };

    /// <summary>
    /// Dictionary, which maps colors to brush types.
    /// </summary>
    private readonly Dictionary<Color, BrushType> _brushTypes = new() {
        { Colors.DarkGreen, BrushType.Forest },
        { Colors.Green, BrushType.Grassland },
        { Colors.PaleGreen, BrushType.Plain },
        { Colors.SaddleBrown, BrushType.Mountain },
        { Colors.Blue, BrushType.Water },
        { Colors.DimGray, BrushType.HighDensityUrbanArea },
        { Colors.DarkGray, BrushType.LowDensityUrbanArea },
        { Colors.White, BrushType.Clear }
    };

    /// <summary>
    /// Dictionary, which maps rectangles to cells.
    /// </summary>
    private Dictionary<Rectangle, Cell> _cells = new();

    /// <summary>
    /// Simulation Charts Window.
    /// </summary>
    private SimulationChartsWindow? _simulationChartsWindow;

    /// <summary>
    /// Task, which runs the simulation.
    /// </summary>
    private Task? _simulationTask;

    /// <summary>
    /// Cancellation token source for the simulation task.
    /// </summary>
    private CancellationTokenSource _simulationCancellationTokenSource = new();

    /// <summary>
    /// Current simulation time.
    /// </summary>
    private int _currentSimulationTime;

    /// <summary>
    /// Indicates whether the simulation is paused.
    /// </summary>
    private bool _isPaused;
    
    /// <summary>
    /// Tool tip for the wind speed slider.
    /// </summary>
    private ToolTip? _toolTip;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationWindow"/> class.
    /// </summary>
    public SimulationWindow() {
        try {
            InitializeComponent();
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while initializing the simulation window." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Initializes cells for each rectangle in the map canvas.
    /// </summary>
    private void InitializeCells() {
        try {
            _cells = new();
            foreach (var row in _rectArray) {
                foreach (var rect in row) {
                    var cell = new Cell {
                        TerrainType = _brushTypes[(rect.Fill as SolidColorBrush)?.Color ?? Colors.White],
                        X = (int)Canvas.GetLeft(rect),
                        Y = (int)Canvas.GetTop(rect)
                    };
                    _cells.Add(rect, cell);
                }
            }
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while initializing cells." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Asynchronous method, responsible for running the simulation and everything related to it.
    /// </summary>
    private async Task SimulateAsync() {
        try {
            var random = new Random();
            _simulationCancellationTokenSource = new CancellationTokenSource();

            while (true) {
                if (_simulationCancellationTokenSource.IsCancellationRequested) {
                    break;
                }

                var simulationSpeedMultiplier = ((ComboBoxItem)SpeedComboBox.SelectedItem).Content.ToString() switch {
                    "x1" => 1,
                    "x2" => 2,
                    "x4" => 4,
                    "x8" => 8,
                    _ => 1
                };
                var simulationSpeed = 1000 / simulationSpeedMultiplier;

                var updatedCells = new List<Tuple<int, int, Rectangle, Cell>>();
                for (var i = 0; i < _rectArray.Count; i++) {
                    for (var j = 0; j < _rectArray[0].Count; j++) {
                        var rect = _rectArray[i][j];
                        var cell = _cells[rect];
                        if (cell.TerrainType is BrushType.Clear or BrushType.Water or BrushType.Mountain) {
                            continue;
                        }

                        switch (cell.BurningState) {
                            case BurningState.Burned:
                                continue;
                            case BurningState.Burning when ++cell.BurningTime > cell.MaximumBurningTime:
                                var newCell = new Cell {
                                    TerrainType = cell.TerrainType,
                                    X = cell.X,
                                    Y = cell.Y,
                                    BurningState = BurningState.Burned
                                };
                                var newRect = new Rectangle {
                                    Width = RectSize,
                                    Height = RectSize,
                                    Fill = new SolidColorBrush(Colors.Black),
                                    Stroke = new SolidColorBrush(Colors.Black),
                                    StrokeThickness = 0
                                };
                                Canvas.SetLeft(newRect, Canvas.GetLeft(rect));
                                Canvas.SetTop(newRect, Canvas.GetTop(rect));
                                updatedCells.Add(new Tuple<int, int, Rectangle, Cell>(i, j, newRect, newCell));
                                continue;
                            case BurningState.Burning:
                                continue;
                        }

                        var neighbours = GetNeighbours(rect);
                        var probability = CalculateProbability(cell, ref neighbours);
                        if (!(random.NextDouble() < probability))
                            continue;

                        var newCell2 = new Cell {
                            TerrainType = cell.TerrainType,
                            X = cell.X,
                            Y = cell.Y,
                            BurningState = BurningState.Burning
                        };
                        var newRect2 = new Rectangle {
                            Width = RectSize,
                            Height = RectSize,
                            Fill = rect.Fill,
                            Stroke = new SolidColorBrush(Colors.OrangeRed),
                            StrokeThickness = 2
                        };
                        Canvas.SetLeft(newRect2, Canvas.GetLeft(rect));
                        Canvas.SetTop(newRect2, Canvas.GetTop(rect));
                        updatedCells.Add(new Tuple<int, int, Rectangle, Cell>(i, j, newRect2, newCell2));
                    }
                }

                if (updatedCells.Count == 0) {
                    if (_cells.Any(cell => cell.Value.BurningState == BurningState.Burning))
                        continue;
                    StopButton_Click(null, null);
                    break;
                }

                foreach (var (i, j, rect, cell) in updatedCells) {
                    Logger.Log($"Cell at ({cell.X}, {cell.Y}) changed its state to {cell.BurningState}.");
                    await Dispatcher.InvokeAsync(() => {
                        SimulationCanvas.Children.Remove(_rectArray[i][j]);
                        _cells.Remove(_rectArray[i][j]);
                        _rectArray[i][j] = rect;
                        _cells[rect] = cell;
                        SimulationCanvas.Children.Add(_rectArray[i][j]);
                    });
                }

                await Dispatcher.InvokeAsync(() => {
                    UpdateSimulationData(ref updatedCells);
                    _simulationChartsWindow?.UpdateCharts();
                });

                await Dispatcher.InvokeAsync(UpdateTime);

                await Task.Delay(simulationSpeed, _simulationCancellationTokenSource.Token);
            }
        }
        catch (TaskCanceledException) {
            Logger.Log("Simulation canceled.");
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while running the simulation." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Updates the simulation data based on the updated cells.
    /// </summary>
    /// <param name="updatedCells">The updated cells.</param>
    private void UpdateSimulationData(ref List<Tuple<int, int, Rectangle, Cell>> updatedCells) {
        try {
            if (_simulationChartsWindow is null)
                return;
            var data = _simulationChartsWindow.Data;
            data.BurningArea.Add(data.BurningArea.Last() +
                                 updatedCells.Count(cell => cell.Item4.BurningState == BurningState.Burning));
            data.BurnedArea.Add(data.BurnedArea.Last() +
                                updatedCells.Count(cell => cell.Item4.BurningState == BurningState.Burned));
            data.BurningArea[^1] -= updatedCells.Count(cell => cell.Item4.BurningState == BurningState.Burned);
            data.BurningSpeed.Add(updatedCells.Count(cell => cell.Item4.BurningState == BurningState.Burning));
            data.DamagedVegetation.Add(data.DamagedVegetation.Last() +
                                       updatedCells.Count(cell => cell.Item4 is {
                                           BurningState: BurningState.Burning,
                                           TerrainType: BrushType.Forest or BrushType.Grassland or BrushType.Plain
                                       }) / data.TotalVegetation * 100);
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while updating the simulation data." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Gets the neighbours of the given rectangle.
    /// </summary>
    /// <param name="rect">The rectangle.</param>
    /// <returns>The neighbours of the given rectangle.</returns>
    private List<Cell> GetNeighbours(Rectangle rect) {
        try {
            var neighbours = new List<Cell>();

            var x = _rectArray.FindIndex(row => row.Contains(rect));
            var y = _rectArray[x].FindIndex(r => r == rect);
            var maxX = _rectArray.Count - 1;
            var maxY = _rectArray[0].Count - 1;

            if (x > 0) {
                neighbours.Add(_cells[_rectArray[x - 1][y]]);
            }

            if (x < maxX) {
                neighbours.Add(_cells[_rectArray[x + 1][y]]);
            }

            if (y > 0) {
                neighbours.Add(_cells[_rectArray[x][y - 1]]);
            }

            if (y < maxY) {
                neighbours.Add(_cells[_rectArray[x][y + 1]]);
            }

            if (x > 0 && y > 0) {
                neighbours.Add(_cells[_rectArray[x - 1][y - 1]]);
            }

            if (x > 0 && y < maxY) {
                neighbours.Add(_cells[_rectArray[x - 1][y + 1]]);
            }

            if (x < maxX && y > 0) {
                neighbours.Add(_cells[_rectArray[x + 1][y - 1]]);
            }

            if (x < maxX && y < maxY) {
                neighbours.Add(_cells[_rectArray[x + 1][y + 1]]);
            }

            return neighbours;
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while getting the neighbours of the given rectangle." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return new List<Cell>();
        }
    }

    /// <summary>
    /// Updates the simulation time.
    /// </summary>
    private void UpdateTime() {
        try {
            var time = TimeSpan.FromSeconds(++_currentSimulationTime);
            TimeTextBlock.Text = time.ToString(@"hh\:mm\:ss");
        }
        catch (Exception exc) {
            Logger.Log(exc);
        }
    }

    /// <summary>
    /// Calculates the probability of the given cell catching fire.
    /// </summary>
    /// <param name="cell">Cell to calculate the probability for.</param>
    /// <param name="neighbours">Neighbours of the cell.</param>
    /// <returns>The probability of the given cell catching fire.</returns>
    private double CalculateProbability(Cell cell, ref List<Cell> neighbours) {
        try {
            // Number of burning neighbours
            var burningNeighbours = neighbours.Count(neighbour => neighbour.BurningState == BurningState.Burning);

            // Wind direction and speed
            var windFactor = GetWindFactor(cell, ref neighbours);

            // Terrain type
            var terrainFactor = GetTerrainFactor(cell.TerrainType);

            // Calculate final probability
            var probability = burningNeighbours * windFactor * terrainFactor;

            // Cap probability to 100%
            probability = Math.Min(1.0, probability);
            
            // Very verbose and resource intensive logging
            // Logger.Log($"Probability of cell {cell.X}, {cell.Y} catching fire: {probability}" +
            //            $"\tBurning neighbours: {burningNeighbours}" +
            //            $"\tWind factor: {windFactor}" +
            //            $"\tTerrain factor: {terrainFactor}");

            return probability;
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while calculating the probability of the given cell catching fire." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return 0;
        }
    }

    /// <summary>
    /// Gets the wind factor for the probability calculation.
    /// </summary>
    /// <param name="cell">Cell to calculate the wind factor for.</param>
    /// <param name="neighbours">Neighbours of the cell.</param>
    /// <returns>The wind factor for the probability calculation.</returns>
    private double GetWindFactor(Cell cell, ref List<Cell> neighbours) {
        try {
            var windSpeed = WindSpeedSlider.Value;
            var windDirection = ((ComboBoxItem)WindDirectionComboBox.SelectedItem).Content.ToString();

            // Calculate wind vector
            var theta = GetWindDirectionAngle(windDirection!);
            var windVectorX = Math.Cos(theta);
            var windVectorY = Math.Sin(theta);

            // Calculate leeward cell
            var leewardX = cell.X + (int)Math.Round(windVectorX);
            var leewardY = cell.Y + (int)Math.Round(windVectorY);

            // Get number of burning neighbouring cells in leeward direction
            var burning = GetBurningNeighbouringCells(ref neighbours, leewardX, leewardY);

            // Calculate wind factor
            var windFactor = Math.Pow((double)burning / neighbours.Count, 2);

            // Apply wind speed factor
            return windFactor * windSpeed * 0.1;
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while getting the wind factor for the probability calculation." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return 0;
        }
    }

    /// <summary>
    /// Gets the wind direction angle for the wind factor calculation.
    /// </summary>
    /// <param name="windDirection">Wind direction.</param>
    /// <returns>The wind direction angle for the wind factor calculation.</returns>
    /// <exception cref="ArgumentException">Thrown when the given wind direction is invalid.</exception>
    private static double GetWindDirectionAngle(string windDirection) {
        return windDirection switch {
            "North" => 0,
            "North-East" => Math.PI / 4,
            "East" => Math.PI / 2,
            "South-East" => 3 * Math.PI / 4,
            "South" => Math.PI,
            "South-West" => 5 * Math.PI / 4,
            "West" => 3 * Math.PI / 2,
            "North-West" => 7 * Math.PI / 4,
            _ => throw new ArgumentException("Invalid wind direction.")
        };
    }

    /// <summary>
    /// Gets the number of burning neighbouring cells in leeward direction.
    /// </summary>
    /// <param name="neighboringCells">Neighbouring cells of the cell to calculate the number of burning neighbouring cells for.</param>
    /// <param name="xLeeward">X coordinate of the leeward cell.</param>
    /// <param name="yLeeward">Y coordinate of the leeward cell.</param>
    /// <returns>The number of burning neighbouring cells in leeward direction.</returns>
    private static int GetBurningNeighbouringCells(ref List<Cell> neighboringCells, int xLeeward, int yLeeward) {
        try {
            return neighboringCells.Where(cell => cell.X != xLeeward || cell.Y != yLeeward)
                                   .Count(cell => cell.BurningState == BurningState.Burning);
        }
        catch (Exception exc) {
            Logger.Log(exc);
            return 0;
        }
    }

    /// <summary>
    /// Gets the terrain factor for the probability calculation.
    /// </summary>
    /// <param name="terrainType">Terrain type.</param>
    /// <returns>The terrain factor for the probability calculation.</returns>
    private static double GetTerrainFactor(BrushType terrainType) {
        var terrainFactor = terrainType switch {
            BrushType.Clear => 0.0,
            BrushType.Forest => 0.8,
            BrushType.Grassland => 0.6,
            BrushType.Plain => 0.4,
            BrushType.Mountain => 0.0,
            BrushType.Water => 0.0,
            BrushType.HighDensityUrbanArea => 0.2,
            BrushType.LowDensityUrbanArea => 0.3,
            _ => 0.0
        };
        return terrainFactor;
    }

    /// <summary>
    /// Handles the click event of the load map menu item. Deserializes a map file and loads it into the simulation.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void LoadMapMenuItem_Click(object sender, RoutedEventArgs e) {
        try {
            var openFileDialog = new OpenFileDialog {
                Filter = "Map files (*.map)|*.map",
                InitialDirectory = Environment.CurrentDirectory
            };
            if (openFileDialog.ShowDialog() != true)
                return;

            var serializer = new XmlSerializer(typeof(List<List<SerializableRectangle>>));
            using var stream = openFileDialog.OpenFile();
            List<List<SerializableRectangle>>? serializableRectArray;
            try {
                serializableRectArray = (List<List<SerializableRectangle>>)serializer.Deserialize(stream);
            }
            catch (Exception exc) {
                Logger.Log(exc);
                serializableRectArray = new List<List<SerializableRectangle>>();
            }

            SimulationCanvas.Children.Clear();

            // Adjust canvas size to map size
            SimulationCanvas.Width = serializableRectArray![0].Count * RectSize;
            SimulationCanvas.Height = serializableRectArray.Count * RectSize;

            _rectArray = new List<List<Rectangle>>();
            foreach (var row in serializableRectArray!) {
                var rectRow = new List<Rectangle>();
                foreach (var serializableRect in row) {
                    var rect = new Rectangle {
                        Width = RectSize,
                        Height = RectSize,
                        Stroke = Brushes.Black,
                        StrokeThickness = 0.25,
                        Fill = _brushes[serializableRect.BrushType]
                    };
                    Canvas.SetLeft(rect, serializableRect.Left);
                    Canvas.SetTop(rect, serializableRect.Top);
                    SimulationCanvas.Children.Add(rect);
                    rectRow.Add(rect);
                }

                _rectArray.Add(rectRow);
            }

            InitializeCells();
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while loading the map." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the mouse down event of the simulation canvas. Sets the burning state of the clicked cell to burning or unburned.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void SimulationCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
        try {
            var mousePosition = e.GetPosition(SimulationCanvas);
            var x = (int)mousePosition.X / RectSize * RectSize;
            var y = (int)mousePosition.Y / RectSize * RectSize;
            var (rect, cell) = _cells.First(pair => pair.Value.X == x && pair.Value.Y == y);
            if (e.LeftButton == MouseButtonState.Pressed) {
                cell.BurningState = BurningState.Burning;
                cell.BurningTime = 0;
                rect.Stroke = Brushes.OrangeRed;
                rect.StrokeThickness = 2;
                return;
            }

            if (e.RightButton != MouseButtonState.Pressed)
                return;
            cell.BurningState = BurningState.Unburned;
            cell.BurningTime = 0;
            rect.Stroke = Brushes.Black;
            rect.StrokeThickness = 0.25;
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while setting the burning state of the cell." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the click event of the start button. Starts the simulation.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void StartButton_Click(object sender, RoutedEventArgs e) {
        try {
            if (_simulationTask is { Status: TaskStatus.Running })
                return;
            if (_isPaused) {
                _isPaused = false;
                _simulationTask = SimulateAsync();
                PauseButton.IsEnabled = true;
                StopButton.IsEnabled = true;
                return;
            }

            if (_cells.All(pair => pair.Value.BurningState != BurningState.Burning)) {
                MessageBox.Show("There must be at least one burning cell to start the simulation." +
                                " Use LMB click to set burning cells.",
                                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _simulationChartsWindow = new SimulationChartsWindow {
                Data = new() {
                    TotalVegetation = _cells.Count(pair => pair.Value.TerrainType is BrushType.Forest
                                                       or BrushType.Grassland
                                                       or BrushType.Plain),
                    BurningArea = { _cells.Count(pair => pair.Value.BurningState == BurningState.Burning) },
                    BurningSpeed = { _cells.Count(pair => pair.Value.BurningState == BurningState.Burning) },
                    BurnedArea = { 0 },
                }
            };
            _simulationChartsWindow.Data.DamagedVegetation.Add(_cells.Count(pair =>
                                                                                pair.Value is {
                                                                                    BurningState: BurningState.Burning,
                                                                                    TerrainType: BrushType.Forest
                                                                                    or BrushType.Grassland
                                                                                    or BrushType.Plain
                                                                                }) / _simulationChartsWindow.Data
                                                                   .TotalVegetation * 100);
            _simulationChartsWindow.Show();
            _simulationTask = SimulateAsync();
            StartButton.Content = "Resume";
            StartButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
            StopButton.IsEnabled = true;
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while starting the simulation." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the click event of the pause button. Pauses the simulation.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void PauseButton_Click(object sender, RoutedEventArgs e) {
        try {
            _simulationCancellationTokenSource.Cancel();
            _isPaused = true;
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while pausing the simulation." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the click event of the stop button. Stops the simulation.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void StopButton_Click(object? sender, RoutedEventArgs? e) {
        try {
            _simulationCancellationTokenSource.Cancel();
            _isPaused = false;
            StartButton.Content = "Start";
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            StopButton.IsEnabled = false;
            OnStop();
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while stopping the simulation." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Displays a message box with the results of the simulation. Asks the user if they want to save the results.
    /// </summary>
    private void OnStop() {
        try {
            if (_simulationChartsWindow is null) {
                MessageBox.Show("Simulation complete.",
                                "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Simulation complete." +
                                         " Would you like to save simulation data and charts?",
                                         "Information", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result != MessageBoxResult.Yes)
                return;
            _simulationChartsWindow.SaveAll();
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while saving simulation data and charts." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the click event of the save parameters menu item. Saves the current simulation parameters to a file.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void SaveParamsMenuItem_OnClick(object sender, RoutedEventArgs e) {
        try {
            var saveFileDialog = new SaveFileDialog {
                Filter = "Simulation parameters (*.json)|*.json",
                InitialDirectory = Environment.CurrentDirectory
            };
            if (saveFileDialog.ShowDialog() != true)
                return;
            var simulationParameters = new SimulationParameters(((ComboBoxItem)SpeedComboBox.SelectedItem).Content.ToString(),
                                                                ((ComboBoxItem)WindDirectionComboBox.SelectedItem).Content.ToString(),
                                                                WindSpeedSlider.Value);
            var json = JsonSerializer.Serialize(simulationParameters);
            File.WriteAllText(saveFileDialog.FileName, json);
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while saving simulation parameters." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the click event of the load parameters menu item. Loads simulation parameters from a file.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void LoadParamsMenuItem_OnClick(object sender, RoutedEventArgs e) {
        try {
            var openFileDialog = new OpenFileDialog {
                Filter = "Simulation parameters (*.json)|*.json",
                InitialDirectory = Environment.CurrentDirectory
            };
            if (openFileDialog.ShowDialog() != true)
                return;
            var json = File.ReadAllText(openFileDialog.FileName);
            var simulationParameters = JsonSerializer.Deserialize<SimulationParameters>(json);
            SpeedComboBox.SelectedItem = SpeedComboBox.Items.Cast<ComboBoxItem>()
                                                      .First(item => item.Content.ToString() ==
                                                                     simulationParameters!.SimulationSpeed);
            WindDirectionComboBox.SelectedItem =
                WindDirectionComboBox.Items.Cast<ComboBoxItem>()
                                     .First(item => item.Content.ToString() ==
                                                    simulationParameters.WindDirection);
            WindSpeedSlider.Value = simulationParameters.WindSpeed;
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while loading simulation parameters." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the click event of the open main menu menu item. Opens the main window.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void OpenMainWindowMenuItem_Click(object? sender, RoutedEventArgs? e) {
        try {
            if (Application.Current.Windows.OfType<MainWindow>().Any()) {
                Application.Current.Windows.OfType<MainWindow>().First().Activate();
                return;
            }

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while opening the main window." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the click event of the exit menu item. Closes the window.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void ExitMenuItem_Click(object sender, RoutedEventArgs e) {
        OnClosing(null);
        Close();
    }

    /// <summary>
    /// Handles the closing event on the window. Opens the main window.
    /// </summary>
    /// <param name="e">Event arguments.</param>
    protected override void OnClosing(CancelEventArgs? e) {
        try {
            OpenMainWindowMenuItem_Click(null, null);
            Application.Current.Windows.OfType<MainWindow>().First().Activate();
        }
        catch (Exception exc) {
            Logger.Log(exc);
        }
    }

    /// <summary>
    /// Handles the click event of the open results menu item. Opens the results folder.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void OpenResultsMenuItem_OnClick(object sender, RoutedEventArgs e) {
        try {
            if (!Directory.Exists("results")) {
                Directory.CreateDirectory("results");
            }

            Process.Start("explorer.exe", "results");
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while opening the results folder." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the mouse enter event of the wind speed slider. Shows a tooltip with the current wind speed.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void WindSpeedSlider_OnMouseEnter(object sender, MouseEventArgs e) {
        try {
            var slider = (Slider)sender;
            _toolTip = new ToolTip {
                Content = $"{slider.Value} m/s",
                PlacementTarget = slider,
                Placement = PlacementMode.Top,
                IsOpen = true
            };
            _toolTip.Closed += (_, _) => _toolTip.IsOpen = false;
        }
        catch (Exception exc) {
            Logger.Log(exc);
        }
    }

    /// <summary>
    /// Handles the mouse leave event of the wind speed slider. Hides the tooltip.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void WindSpeedSlider_OnMouseLeave(object sender, MouseEventArgs e) {
        try {
            if (_toolTip is null)
                return;
            _toolTip.IsOpen = false;
        }
        catch (Exception exc) {
            Logger.Log(exc);
        }
    }

    /// <summary>
    /// Handles the value changed event of the wind speed slider. Updates the tooltip with the current wind speed.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void WindSpeedSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
        try {
            if (_toolTip is null)
                return;
            _toolTip.Content = $"{e.NewValue} m/s";
        }
        catch (Exception exc) {
            Logger.Log(exc);
        }
    }
}
