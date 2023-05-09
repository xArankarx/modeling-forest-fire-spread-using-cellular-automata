using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
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

    private Task? _simulationTask;

    private CancellationTokenSource _simulationCancellationTokenSource;

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
                            // cell.BurningState = BurningState.Burned;
                            // await Dispatcher.InvokeAsync(() => {
                            //     rect.Fill = new SolidColorBrush(Colors.Black);
                            //     rect.StrokeThickness = 0;
                            // });
                            var newCell = new Cell {
                                TerrainType = cell.TerrainType,
                                X = cell.X,
                                Y = cell.Y
                            };
                            newCell.BurningState = BurningState.Burned;
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

                    var neighbours = GetNeighbours(cell, rect);
                    // TODO: Add probability of extinguishing
                    var probability = UpdateProbability(cell, ref neighbours);
                    if (!(random.NextDouble() < probability))
                        continue;
                    // cell.BurningState = BurningState.Burning;
                    // await Dispatcher.InvokeAsync(() => {
                    //     rect.Stroke = new SolidColorBrush(Colors.Red);
                    //     rect.StrokeThickness = 2;
                    // });
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
                        Stroke = new SolidColorBrush(Colors.Red),
                        StrokeThickness = 2
                    };
                    Canvas.SetLeft(newRect2, Canvas.GetLeft(rect));
                    Canvas.SetTop(newRect2, Canvas.GetTop(rect));
                    updatedCells.Add(new Tuple<int, int, Rectangle, Cell>(i, j, newRect2, newCell2));
                }
            }
            
            if (updatedCells.Count == 0)
                // TODO: Display info about simulation end, disable buttons, write data and charts to files
                break;
            
            foreach (var (i, j, rect, cell) in updatedCells) {
                await Dispatcher.InvokeAsync(() => {
                    SimulationCanvas.Children.Remove(_rectArray[i][j]);
                    _rectArray[i][j] = rect;
                    _cells[rect] = cell;
                    SimulationCanvas.Children.Add(_rectArray[i][j]);
                });
            }

            await Dispatcher.InvokeAsync(UpdateTime);
            
            await Task.Delay(simulationSpeed, _simulationCancellationTokenSource.Token);
        }
    }

    private List<Cell> GetNeighbours(Cell cell, Rectangle rect) {
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

    private double UpdateProbability(Cell cell, ref List<Cell> neighbours) {
        var burningNeighbours = neighbours.Count(neighbour => neighbour.BurningState == BurningState.Burning);

        // Wind direction and speed
        var windFactor = GetWindFactor(cell, ref neighbours);

        // Terrain type
        var terrainFactor = GetTerrainFactor(cell.TerrainType);

        // Ignition factor
        var ignitionFactor = GetIgnitionFactor(ref neighbours);

        // Compute final probability
        var probability = burningNeighbours * windFactor * terrainFactor + ignitionFactor;

        // Cap probability to 100%
        probability = Math.Min(1.0, probability);

        return probability;
    }

    private double GetWindFactor(Cell cell, ref List<Cell> neighbours) {
        // take into account all burning neighbours from the given direction
        var windSpeed = WindSpeedSlider.Value;
        var windDirection =  ((ComboBoxItem) WindDirectionComboBox.SelectedItem).Content.ToString();
        var burningNeighbours = neighbours.Where(neighbour => neighbour.BurningState == BurningState.Burning).ToList();
        Dictionary<string, double> windFactors = new() {
            { "North", burningNeighbours.Count(neighbour => cell.Y < neighbour.Y && neighbour.X == cell.X) },
            { "North-East", burningNeighbours.Count(neighbour => cell.Y < neighbour.Y && neighbour.X < cell.X) },
            { "East", burningNeighbours.Count(neighbour => neighbour.X < cell.X && neighbour.Y == cell.Y) },
            { "South-East", burningNeighbours.Count(neighbour => cell.Y > neighbour.Y && neighbour.X < cell.X) },
            { "South", burningNeighbours.Count(neighbour => cell.Y > neighbour.Y && neighbour.X == cell.X) },
            { "South-West", burningNeighbours.Count(neighbour => cell.Y > neighbour.Y && neighbour.X > cell.X) },
            { "West", burningNeighbours.Count(neighbour => neighbour.X > cell.X && neighbour.Y == cell.Y) },
            { "North-West", burningNeighbours.Count(neighbour => cell.Y < neighbour.Y && neighbour.X > cell.X) }
        };
        // TODO: refactor windFactor
        var windFactor = windFactors[windDirection!] + windSpeed * 0.01;
        return windFactor;
    }

    private static double GetTerrainFactor(BrushType terrainType) {
        var terrainFactor = terrainType switch {
            BrushType.Clear => 0.0,
            BrushType.Forest => 0.7,
            BrushType.Grassland => 0.45,
            BrushType.Plain => 0.1,
            BrushType.Mountain => 0.0,
            BrushType.Water => 0.0,
            BrushType.HighDensityUrbanArea => 0.05,
            BrushType.LowDensityUrbanArea => 0.1,
            _ => 0.0
        };
        return terrainFactor;
    }

    private static double GetIgnitionFactor(ref List<Cell> neighbours) {
        var ignitionFactor = neighbours.Count(neighbour => neighbour.BurningState == BurningState.Burning) * 0.05;
        return ignitionFactor;
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
        foreach (var row in serializableRectArray) {
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
            rect.Stroke = Brushes.Red;
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

    private void StopButton_Click(object sender, RoutedEventArgs e) {
        _simulationCancellationTokenSource.Cancel();
        _isPaused = false;
        StartButton.Content = "Start";
        StartButton.IsEnabled = true;
        PauseButton.IsEnabled = false;
        StopButton.IsEnabled = false;
    }
}
