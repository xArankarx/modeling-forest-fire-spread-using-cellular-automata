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
using MFFSuCA.Utility;
using Microsoft.Win32;

namespace MFFSuCA.Windows;

/// <summary>
/// Interaction logic for MapConstructorWindow.xaml
/// </summary>
public partial class MapConstructorWindow {
    /// <summary>
    /// Size of one rectangle in the map canvas.
    /// </summary>
    private const int RectSize = 10;

    /// <summary>
    /// Height of the map canvas.
    /// </summary>
    private int _height;

    /// <summary>
    /// Width of the map canvas.
    /// </summary>
    private int _width;

    /// <summary>
    /// List of rectangles in the map canvas.
    /// </summary>
    private List<List<Rectangle>> _rectArray = new();

    /// <summary>
    /// Brush indicator.
    /// </summary>
    private Ellipse _brushIndicator = new();

    /// <summary>
    /// Current brush type.
    /// </summary>
    private BrushType _currentBrushType = BrushType.Forest;

    /// <summary>
    /// Current brush size.
    /// </summary>
    private int _currentBrushSize = 10;

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
    /// Initializes a new instance of the <see cref="MapConstructorWindow"/> class.
    /// </summary>
    public MapConstructorWindow() {
        try {
            InitializeComponent();
            CreateBrushIndicator();
            BrushSizeSlider.ValueChanged += BrushSizeSlider_ValueChanged;
            MapSizeComboBox.SelectionChanged += MapSizeComboBox_OnSelectionChanged;
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while initializing the map constructor window." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Initializes the map canvas with rectangles.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void MapCanvas_Initialized(object? sender, EventArgs? e) {
        try {
            _height = (int)MapCanvas.Height / RectSize;
            _width = (int)MapCanvas.Width / RectSize;

            _rectArray = new List<List<Rectangle>> {
                Capacity = _height
            };

            for (var i = 0; i < _height; i++) {
                _rectArray.Add(new List<Rectangle>());
                _rectArray[i].Capacity = _width;
                for (var j = 0; j < _width; j++) {
                    var rect = new Rectangle {
                        Width = RectSize,
                        Height = RectSize,
                        Stroke = Brushes.Black,
                        StrokeThickness = 0.25,
                        Fill = Brushes.White
                    };
                    Canvas.SetLeft(rect, j * RectSize);
                    Canvas.SetTop(rect, i * RectSize);
                    MapCanvas.Children.Add(rect);

                    _rectArray[i].Add(rect);
                }
            }
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while initializing the map canvas." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Creates a brush indicator and adds it to the map canvas.
    /// </summary>
    private void CreateBrushIndicator() {
        try {
            _brushIndicator = new Ellipse {
                Width = _currentBrushSize,
                Height = _currentBrushSize,
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Opacity = 0.5
            };
            MapCanvas.Children.Add(_brushIndicator);
        }
        catch (Exception exc) {
            Logger.Log(exc);
        }
    }

    /// <summary>
    /// Updates the brush indicator size and position.
    /// </summary>
    /// <param name="position">Mouse position.</param>
    private void UpdateBrushIndicator(Point position) {
        try {
            Canvas.SetLeft(_brushIndicator!, position.X - _currentBrushSize / 2.0);
            Canvas.SetTop(_brushIndicator!, position.Y - _currentBrushSize / 2.0);
        }
        catch (Exception exc) {
            Logger.Log(exc);
        }
    }

    /// <summary>
    /// Draws rectangles in the map canvas based on the current brush type and size.
    /// </summary>
    /// <param name="position">Mouse position.</param>
    /// <param name="brushType">Brush type.</param>
    private void DrawBrush(Point position, BrushType brushType) {
        try {
            for (var i = 0; i < _height; i++) {
                for (var j = 0; j < _width; j++) {
                    var rect = _rectArray[i][j];

                    var distance = Math.Sqrt(Math.Pow(Canvas.GetLeft(rect) + rect.Width / 2.0 - position.X, 2) +
                                             Math.Pow(Canvas.GetTop(rect) + rect.Height / 2.0 - position.Y, 2));

                    if (distance < _currentBrushSize / 2.0) {
                        rect.Fill = _brushes[brushType];
                    }
                }
            }
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while drawing with the brush." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the mouse click event on the forest brush button.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void ForestBrushButton_Click(object sender, RoutedEventArgs e) => _currentBrushType = BrushType.Forest;

    /// <summary>
    /// Handles the mouse click event on the grassland brush button.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void GrasslandBrushButton_Click(object sender, RoutedEventArgs e) =>
        _currentBrushType = BrushType.Grassland;

    /// <summary>
    /// Handles the mouse click event on the plain brush button.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void PlainBrushButton_Click(object sender, RoutedEventArgs e) => _currentBrushType = BrushType.Plain;

    /// <summary>
    /// Handles the mouse click event on the mountain brush button.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void MountainBrushButton_Click(object sender, RoutedEventArgs e) => _currentBrushType = BrushType.Mountain;

    /// <summary>
    /// Handles the mouse click event on the water brush button.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void WaterBrushButton_Click(object sender, RoutedEventArgs e) => _currentBrushType = BrushType.Water;

    /// <summary>
    /// Handles the mouse click event on the high density urban area brush button.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void HighDensityUrbanBrushButton_Click(object sender, RoutedEventArgs e) =>
        _currentBrushType = BrushType.HighDensityUrbanArea;

    /// <summary>
    /// Handles the mouse click event on the low density urban area brush button.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void LowDensityUrbanBrushButton_Click(object sender, RoutedEventArgs e) =>
        _currentBrushType = BrushType.LowDensityUrbanArea;

    /// <summary>
    /// Handles the mouse click event on the clear brush button.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void ClearBrushButton_Click(object sender, RoutedEventArgs e) => _currentBrushType = BrushType.Clear;

    /// <summary>
    /// Handles the mouse down event on the map canvas.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void MapCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
        try {
            MapCanvas.CaptureMouse();
            MapCanvas.MouseMove += MapCanvas_MouseMove;
            DrawBrush(e.GetPosition(MapCanvas), _currentBrushType);
        }
        catch (Exception exc) {
            Logger.Log(exc);
        }
    }

    /// <summary>
    /// Handles the mouse move event on the map canvas.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void MapCanvas_MouseMove(object sender, MouseEventArgs e) {
        try {
            var position = e.GetPosition(MapCanvas);
            DrawBrush(position, _currentBrushType);
        }
        catch (Exception exc) {
            Logger.Log(exc);
        }
    }

    /// <summary>
    /// Handles the mouse enter event on the map canvas.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void MapCanvas_MouseEnter(object sender, MouseEventArgs e) => MapCanvas.MouseMove += MapCanvas_MouseOn;

    /// <summary>
    /// Handles the mouse on event on the map canvas.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void MapCanvas_MouseOn(object sender, MouseEventArgs e) => UpdateBrushIndicator(e.GetPosition(MapCanvas));

    /// <summary>
    /// Handles the mouse leave event on the map canvas.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void MapCanvas_MouseLeave(object sender, MouseEventArgs e) {
        try {
            MapCanvas.ReleaseMouseCapture();
            MapCanvas.MouseMove -= MapCanvas_MouseOn;
        }
        catch (Exception exc) {
            Logger.Log(exc);
        }
    }

    /// <summary>
    /// Handles the mouse up event on the map canvas.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void MapCanvas_MouseUp(object sender, MouseButtonEventArgs e) {
        try {
            MapCanvas.ReleaseMouseCapture();
            MapCanvas.MouseMove -= MapCanvas_MouseMove;
        }
        catch (Exception exc) {
            Logger.Log(exc);
        }
    }

    /// <summary>
    /// Handles the value changed event on the brush size slider.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void BrushSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
        _currentBrushSize = (int)e.NewValue;
        _brushIndicator.Width = _currentBrushSize;
        _brushIndicator.Height = _currentBrushSize;
    }

    /// <summary>
    /// Handles the click event on the save button. Serializes the map to a file.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void SaveButton_Click(object sender, RoutedEventArgs e) {
        try {
            var saveFileDialog = new SaveFileDialog {
                Filter = "Map files (*.map)|*.map",
                InitialDirectory = Environment.CurrentDirectory
            };
            if (saveFileDialog.ShowDialog() != true)
                return;

            var serializableRectArray = _rectArray
                                        .Select(row => row
                                                       .Select(rect => new SerializableRectangle(Canvas.GetLeft(rect),
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
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while saving the map." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the click event on the load button. Deserializes the map from a file.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void LoadButton_Click(object sender, RoutedEventArgs e) {
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
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while loading the map." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the click event on the open main menu menu item. Opens the main window.
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
    /// Handles the click event on the exit menu item. Closes this window.
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
    /// Handles the selection changed event on the map size combo box. Changes the map size.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void MapSizeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
        try {
            if (MapCanvas == null)
                return;

            var selectedMapSize = ((ComboBoxItem)MapSizeComboBox.SelectedItem).Content.ToString().Split('x');
            MapCanvas.Width = int.Parse(selectedMapSize[0]);
            MapCanvas.Height = int.Parse(selectedMapSize[1]);
            MapCanvas.Children.Clear();
            MapCanvas_Initialized(null, null);
            CreateBrushIndicator();
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while changing the map size." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
