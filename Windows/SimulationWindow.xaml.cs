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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Serialization;
using MFFSuCA.Enums;
using MFFSuCA.Models;
using Microsoft.Win32;

namespace MFFSuCA.Windows;

public partial class SimulationWindow {
    private const int RectSize = 10;

    private List<List<Rectangle>> _rectArray = new();

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

    private Dictionary<Rectangle, Cell> _cells = new();

    private SimulationChartsWindow? _simulationChartsWindow;

    private Task? _simulationTask;

    private CancellationTokenSource _simulationCancellationTokenSource = new();

    private int _currentSimulationTime;

    private bool _isPaused;

    public SimulationWindow() {
        InitializeComponent();
    }

    private void InitializeCells() {
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

    private async Task SimulateAsync() {
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

    private void UpdateSimulationData(ref List<Tuple<int, int, Rectangle, Cell>> updatedCells) {
        if (_simulationChartsWindow is null)
            return;
        _simulationChartsWindow.Data.BurningArea.Add(_simulationChartsWindow.Data.BurningArea.Last() +
                                                     updatedCells.Count(cell => cell.Item4.BurningState ==
                                                                            BurningState.Burning));
        _simulationChartsWindow.Data.BurnedArea.Add(_simulationChartsWindow.Data.BurnedArea.Last() +
                                                    updatedCells.Count(cell => cell.Item4.BurningState ==
                                                                               BurningState.Burned));
        _simulationChartsWindow.Data.BurningArea[^1] -= updatedCells.Count(cell => cell.Item4.BurningState ==
                                                                               BurningState.Burned);
        _simulationChartsWindow.Data.BurningSpeed.Add(updatedCells.Count(cell => cell.Item4.BurningState ==
                                                                             BurningState.Burning));
        _simulationChartsWindow.Data.DamagedVegetation.Add(_simulationChartsWindow.Data.DamagedVegetation.Last() +
                                                           updatedCells.Count(cell => cell.Item4 is {
                                                               BurningState: BurningState.Burning,
                                                               TerrainType: BrushType.Forest or BrushType.Grassland
                                                               or BrushType.Plain
                                                           }) / _simulationChartsWindow.Data.TotalVegetation * 100);
    }

    private List<Cell> GetNeighbours(Rectangle rect) {
        try {
            var neighbours = new List<Cell>();
            // find indexes of rect in _rectArray
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
        catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }

    private void UpdateTime() {
        var time = TimeSpan.FromSeconds(++_currentSimulationTime);
        TimeTextBlock.Text = time.ToString(@"hh\:mm\:ss");
    }

    private double CalculateProbability(Cell cell, ref List<Cell> neighbours) {
        var burningNeighbours = neighbours.Count(neighbour => neighbour.BurningState == BurningState.Burning);

        // Wind direction and speed
        var windFactor = GetWindFactor(cell, ref neighbours);

        // Terrain type
        var terrainFactor = GetTerrainFactor(cell.TerrainType);

        // Compute final probability
        var probability = burningNeighbours * windFactor * terrainFactor;

        // Cap probability to 100%
        probability = Math.Min(1.0, probability);

        return probability;
    }
    
    

    private double GetWindFactor(Cell cell, ref List<Cell> neighbours) {
        var windSpeed = WindSpeedSlider.Value;
        var windDirection = ((ComboBoxItem)WindDirectionComboBox.SelectedItem).Content.ToString();
        
        var theta = GetWindDirectionAngle(windDirection!);
        var windVectorX = Math.Cos(theta);
        var windVectorY = Math.Sin(theta);

        var leewardX = cell.X + (int)Math.Round(windVectorX);
        var leewardY = cell.Y + (int)Math.Round(windVectorY);
        
        var burning = GetBurningNeighbouringCells(ref neighbours, leewardX, leewardY);
        
        var windFactor = Math.Pow((double)burning / neighbours.Count, 2);
        
        return windFactor * windSpeed * 0.1;
    }
    
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
    
    private static int GetBurningNeighbouringCells(ref List<Cell> neighboringCells, int xLeeward, int yLeeward) {
        return neighboringCells.Where(cell => cell.X != xLeeward || cell.Y != yLeeward).Count(cell => cell.BurningState == BurningState.Burning);
    }

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

    private void LoadMapMenuItem_Click(object sender, RoutedEventArgs e) {
        // deserialize the array of rectangles from a selected file
        var openFileDialog = new OpenFileDialog {
            Filter = "Map files (*.map)|*.map",
            InitialDirectory = Environment.CurrentDirectory
        };
        if (openFileDialog.ShowDialog() != true)
            return;

        var serializer = new XmlSerializer(typeof(List<List<SerializableRectangle>>));
        using var stream = openFileDialog.OpenFile();
        var serializableRectArray = (List<List<SerializableRectangle>>)serializer.Deserialize(stream);

        SimulationCanvas.Children.Clear();
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

    private void SimulationCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
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

    private void StartButton_Click(object sender, RoutedEventArgs e) {
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
                TotalVegetation =
                    _cells.Count(pair => pair.Value.TerrainType is BrushType.Forest or BrushType.Grassland
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

    private void PauseButton_Click(object sender, RoutedEventArgs e) {
        _simulationCancellationTokenSource.Cancel();
        _isPaused = true;
        StartButton.IsEnabled = true;
        PauseButton.IsEnabled = false;
        StopButton.IsEnabled = true;
    }

    private void StopButton_Click(object? sender, RoutedEventArgs? e) {
        _simulationCancellationTokenSource.Cancel();
        _isPaused = false;
        StartButton.Content = "Start";
        StartButton.IsEnabled = true;
        PauseButton.IsEnabled = false;
        StopButton.IsEnabled = false;
        OnStop();
    }

    private void OnStop() {
        if (_simulationChartsWindow is null) {
            MessageBox.Show("Simulation complete.",
                            "Information",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show("Simulation complete. Would you like to save simulation data and charts?",
                                     "Information",
                                     MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (result != MessageBoxResult.Yes)
            return;
        _simulationChartsWindow.SaveAll();
    }

    private void SaveParamsMenuItem_OnClick(object sender, RoutedEventArgs e) {
        var saveFileDialog = new SaveFileDialog {
            Filter = "Simulation parameters (*.json)|*.json",
            InitialDirectory = Environment.CurrentDirectory
        };
        if (saveFileDialog.ShowDialog() != true)
            return;
        var simulationParameters =
            new SimulationParameters(((ComboBoxItem)SpeedComboBox.SelectedItem).Content.ToString(),
                                     ((ComboBoxItem)WindDirectionComboBox.SelectedItem).Content.ToString(),
                                     WindSpeedSlider.Value);
        var json = JsonSerializer.Serialize(simulationParameters);
        File.WriteAllText(saveFileDialog.FileName, json);
    }

    private void LoadParamsMenuItem_OnClick(object sender, RoutedEventArgs e) {
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

    private void OpenMainWindowMenuItem_Click(object? sender, RoutedEventArgs? e) {
        if (Application.Current.Windows.OfType<MainWindow>().Any()) {
            Application.Current.Windows.OfType<MainWindow>().First().Activate();
            return;
        }
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e) {
        OnClosing(null);
        Close();
    }

    protected override void OnClosing(CancelEventArgs? e) {
        OpenMainWindowMenuItem_Click(null, null);
        Application.Current.Windows.OfType<MainWindow>().First().Activate();
    }

    private void OpenResultsMenuItem_OnClick(object sender, RoutedEventArgs e) {
        if (!Directory.Exists("results")) {
            Directory.CreateDirectory("results");
        }
        Process.Start("explorer.exe", "results");
    }
}
