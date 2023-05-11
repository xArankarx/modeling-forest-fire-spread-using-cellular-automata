using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

public partial class MapConstructorWindow {
    private const int RectSize = 10;

    private int _height;
    private int _width;
    private List<List<Rectangle>> _rectArray = new();

    private Ellipse? _brushIndicator;
    private BrushType _currentBrushType = BrushType.Forest;
    private int _currentBrushSize = 10;

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

    public MapConstructorWindow() {
        InitializeComponent();
        CreateBrushIndicator();
        BrushSizeSlider.ValueChanged += BrushSizeSlider_ValueChanged;
    }

    private void MapCanvas_Initialized(object sender, EventArgs? e) {
        _height = (int)MapCanvas.Height / RectSize;
        _width = (int)MapCanvas.Width / RectSize;

        // Create a two-dimensional array of Rectangle objects
        _rectArray = new List<List<Rectangle>> {
            Capacity = _height
        };

        // Loop through the array and create a new rectangle for each element
        for (var i = 0; i < _height; i++) {
            _rectArray.Add(new List<Rectangle>());
            _rectArray[i].Capacity = _width;
            for (var j = 0; j < _width; j++) {
                var rect = new Rectangle {
                    Width = RectSize,
                    Height = RectSize,
                    Stroke = Brushes.Black,
                    StrokeThickness = 0.25,
                    Fill = Brushes.White // Start with all rectangles being white
                };
                Canvas.SetLeft(rect, j * RectSize);
                Canvas.SetTop(rect, i * RectSize);
                MapCanvas.Children.Add(rect);

                // Add the rectangle to the array
                _rectArray[i].Add(rect);
            }
        }
    }

    private void CreateBrushIndicator() {
        _brushIndicator = new Ellipse {
            Width = _currentBrushSize,
            Height = _currentBrushSize,
            Stroke = Brushes.Black,
            StrokeThickness = 1,
            Opacity = 0.5
        };
        MapCanvas.Children.Add(_brushIndicator);
    }

    private void UpdateBrushIndicator(Point position) {
        Canvas.SetLeft(_brushIndicator!, position.X - _currentBrushSize / 2.0);
        Canvas.SetTop(_brushIndicator!, position.Y - _currentBrushSize / 2.0);
    }

    private void DrawBrush(Point position, BrushType brushType) {
        // Loop through the array of rectangles and set the color of each one within the brush radius
        for (var i = 0; i < _height; i++) {
            for (var j = 0; j < _width; j++) {
                var rect = _rectArray[i][j];

                // Calculate the distance between the center of the rectangle and the mouse position
                var distance = Math.Sqrt(Math.Pow(Canvas.GetLeft(rect) + rect.Width / 2.0 - position.X, 2) +
                                         Math.Pow(Canvas.GetTop(rect) + rect.Height / 2.0 - position.Y, 2));

                // If the distance is within the brush radius, set the color of the rectangle
                if (distance < _currentBrushSize / 2.0) {
                    rect.Fill = _brushes[brushType];
                }
            }
        }
    }

    private void ForestBrushButton_Click(object sender, RoutedEventArgs e) {
        _currentBrushType = BrushType.Forest;
    }

    private void GrasslandBrushButton_Click(object sender, RoutedEventArgs e) {
        _currentBrushType = BrushType.Grassland;
    }

    private void PlainBrushButton_Click(object sender, RoutedEventArgs e) {
        _currentBrushType = BrushType.Plain;
    }

    private void MountainBrushButton_Click(object sender, RoutedEventArgs e) {
        _currentBrushType = BrushType.Mountain;
    }

    private void WaterBrushButton_Click(object sender, RoutedEventArgs e) {
        _currentBrushType = BrushType.Water;
    }

    private void HighDensityUrbanBrushButton_Click(object sender, RoutedEventArgs e) {
        _currentBrushType = BrushType.HighDensityUrbanArea;
    }

    private void LowDensityUrbanBrushButton_Click(object sender, RoutedEventArgs e) {
        _currentBrushType = BrushType.LowDensityUrbanArea;
    }

    private void ClearBrushButton_Click(object sender, RoutedEventArgs e) {
        _currentBrushType = BrushType.Clear;
    }

    private void MapCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
        MapCanvas.CaptureMouse(); // Capture the mouse to receive MouseMove events
        MapCanvas.MouseMove += MapCanvas_MouseMove; // Subscribe to the MouseMove event
        DrawBrush(e.GetPosition(MapCanvas), _currentBrushType); // Draw the brush
    }

    private void MapCanvas_MouseMove(object sender, MouseEventArgs e) {
        var position = e.GetPosition(MapCanvas); // Get the mouse position relative to the canvas
        DrawBrush(position, _currentBrushType); // Draw the brush
    }

    private void MapCanvas_MouseEnter(object sender, MouseEventArgs e) {
        MapCanvas.MouseMove += MapCanvas_MouseOn; // Subscribe to the MouseMove event
    }

    private void MapCanvas_MouseOn(object sender, MouseEventArgs e) {
        UpdateBrushIndicator(e.GetPosition(MapCanvas)); // Update the brush indicator position
    }

    private void MapCanvas_MouseLeave(object sender, MouseEventArgs e) {
        MapCanvas.MouseMove -= MapCanvas_MouseOn; // Unsubscribe from the MouseMove event
    }

    private void MapCanvas_MouseUp(object sender, MouseButtonEventArgs e) {
        MapCanvas.ReleaseMouseCapture(); // Stop capturing the mouse
        MapCanvas.MouseMove -= MapCanvas_MouseMove; // Unsubscribe from the MouseMove event
    }

    private void BrushSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
        _currentBrushSize = (int)e.NewValue;
        _brushIndicator!.Width = _currentBrushSize;
        _brushIndicator.Height = _currentBrushSize;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e) {
        // serialize the array of rectangles to a selected file
        var saveFileDialog = new SaveFileDialog {
            Filter = "Map files (*.map)|*.map",
            InitialDirectory = Environment.CurrentDirectory
        };
        if (saveFileDialog.ShowDialog() != true)
            return;

        var serializableRectArray = _rectArray
                                    .Select(row => row.Select(rect => new SerializableRectangle(Canvas.GetLeft(rect),
                                                                  Canvas.GetTop(rect),
                                                                  _brushes
                                                                      .FirstOrDefault(x => x.Value == rect.Fill)
                                                                      .Key))
                                                      .ToList())
                                    .ToList();

        var serializer = new XmlSerializer(typeof(List<List<SerializableRectangle>>));
        using var stream = saveFileDialog.OpenFile();
        serializer.Serialize(stream, serializableRectArray);
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e) {
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

        MapCanvas.Children.Clear();
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
                MapCanvas.Children.Add(rect);
                rectRow.Add(rect);
            }

            _rectArray.Add(rectRow);
        }
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
}
