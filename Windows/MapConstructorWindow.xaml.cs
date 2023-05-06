using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MFFSuCA.Enums;

namespace MFFSuCA.Windows;

public partial class MapConstructorWindow : Window {
    private BrushType _currentBrushType = BrushType.Forest;
    private int _currentBrushSize = 10;
    private Ellipse? _brushIndicator;

    private readonly Dictionary<BrushType, Brush> _brushes = new() {
        { BrushType.Forest, new SolidColorBrush(Colors.Green) },
        { BrushType.Mountain, new SolidColorBrush(Colors.SaddleBrown) },
        { BrushType.Water, new SolidColorBrush(Colors.Blue) },
        { BrushType.Clear, new SolidColorBrush(Colors.White) }
    };

    private readonly Dictionary<BrushType, List<Ellipse>> _brushShapes = new() {
        { BrushType.Forest, new List<Ellipse>() },
        { BrushType.Mountain, new List<Ellipse>() },
        { BrushType.Water, new List<Ellipse>() },
        { BrushType.Clear, new List<Ellipse>() }
    };

    public MapConstructorWindow() {
        InitializeComponent();
        CreateBrushIndicator();
        CreateBrushShapes();
    }

    // private void DrawBrush(Point position, int brushSize, BrushType brushType) {
    //     // Create a new brush based on the current brush type
    //     var brush = new SolidColorBrush();
    //     brush.Color = brushType switch {
    //         BrushType.Forest => Colors.Green,
    //         BrushType.Mountain => Colors.SaddleBrown,
    //         BrushType.Water => Colors.Blue,
    //         BrushType.Clear => Colors.White,
    //         _ => brush.Color
    //     };
    //
    //     // Find any existing rectangles at the brush position
    //     var existingRectangles = MapCanvas.Children.OfType<Rectangle>()
    //                                       .Where(r => r.Fill is SolidColorBrush colorBrush && colorBrush.Color != Colors.White
    //                                                  && Canvas.GetLeft(r) >= position.X - brushSize && Canvas.GetLeft(r) <= position.X + brushSize
    //                                                  && Canvas.GetTop(r) >= position.Y - brushSize && Canvas.GetTop(r) <= position.Y + brushSize)
    //                                       .ToList();
    //
    //     if (existingRectangles.Count > 0) {
    //         // Change the color of any existing rectangles to match the new brush color
    //         existingRectangles.ForEach(r => ((SolidColorBrush)r.Fill).Color = brush.Color);
    //     } else {
    //         // Create a new rectangle with the brush and size
    //         var rect = new Rectangle {
    //             Width = brushSize,
    //             Height = brushSize,
    //             Fill = brush
    //         };
    //
    //         // Set the position of the rectangle based on the mouse click position
    //         Canvas.SetLeft(rect, position.X - (brushSize / 2.0));
    //         Canvas.SetTop(rect, position.Y - (brushSize / 2.0));
    //
    //         // Add the rectangle to the canvas
    //         MapCanvas.Children.Add(rect);
    //     }
    // }

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

    private void CreateBrushShapes() {
        foreach (var brushType in _brushes.Keys) {
            for (var i = 0; i < 100; i++) {
                // create 100 brush shapes per type
                var brushShape = new Ellipse {
                    Width = _currentBrushSize,
                    Height = _currentBrushSize,
                    Fill = _brushes[brushType]
                };
                _brushShapes[brushType].Add(brushShape);
                MapCanvas.Children.Add(brushShape);
                brushShape.Visibility = Visibility.Hidden;
            }
        }
    }

    private void UpdateBrushIndicator(Point position) {
        Canvas.SetLeft(_brushIndicator!, position.X - (_currentBrushSize / 2.0));
        Canvas.SetTop(_brushIndicator!, position.Y - (_currentBrushSize / 2.0));
    }

    // private void DrawBrush(Point position, int brushSize, BrushType brushType) {
    //     // Create a new brush based on the current brush type
    //     var brush = new SolidColorBrush();
    //     brush.Color = brushType switch {
    //         BrushType.Forest => Colors.Green,
    //         BrushType.Mountain => Colors.SaddleBrown,
    //         BrushType.Water => Colors.Blue,
    //         BrushType.Clear => Colors.White,
    //         _ => brush.Color
    //     };
    //
    //     if (_brushSizeIndicator == null) {
    //         // Create a new ellipse to indicate the brush size
    //         _brushSizeIndicator = new Ellipse {
    //             Stroke = brush,
    //             StrokeThickness = 2,
    //             Width = brushSize,
    //             Height = brushSize,
    //             Opacity = 0.5,
    //             IsHitTestVisible = false // Ignore mouse events on the indicator
    //         };
    //
    //         // Add the indicator to the canvas
    //         MapCanvas.Children.Add(_brushSizeIndicator);
    //     }
    //     else {
    //         // Update the size and position of the ellipse
    //         _brushSizeIndicator.Width = brushSize;
    //         _brushSizeIndicator.Height = brushSize;
    //     }
    //
    //     // Set the position of the ellipse based on the mouse click position
    //     Canvas.SetLeft(_brushSizeIndicator, position.X - (brushSize / 2.0));
    //     Canvas.SetTop(_brushSizeIndicator, position.Y - (brushSize / 2.0));
    //
    //     // Get the coordinates of the area to fill with the brush
    //     var left = (int)(position.X - (brushSize / 2.0));
    //     var top = (int)(position.Y - (brushSize / 2.0));
    //     var right = (int)(position.X + (brushSize / 2.0));
    //     var bottom = (int)(position.Y + (brushSize / 2.0));
    //
    //     // Make sure the coordinates are within the canvas bounds
    //     if (left < 0) left = 0;
    //     if (top < 0) top = 0;
    //     if (right > MapCanvas.ActualWidth) right = (int)MapCanvas.ActualWidth;
    //     if (bottom > MapCanvas.ActualHeight) bottom = (int)MapCanvas.ActualHeight;
    //
    //     // Fill the area with the brush
    //     for (int x = left; x < right; x++) {
    //         for (int y = top; y < bottom; y++) {
    //             // Create a new rectangle with the brush color and size
    //             // TODO: optimize this by reusing rectangles instead of creating new ones
    //             var rect = new Rectangle {
    //                 Width = 1,
    //                 Height = 1,
    //                 Fill = brush
    //             };
    //
    //             // Set the position of the rectangle
    //             Canvas.SetLeft(rect, x);
    //             Canvas.SetTop(rect, y);
    //
    //             // Add the rectangle to the canvas
    //             MapCanvas.Children.Add(rect);
    //         }
    //     }
    // }
    
    private void DrawBrush(Point position, BrushType brushType) {
        foreach (var brushShape in _brushShapes[brushType]) {
            if (brushShape.Visibility == Visibility.Hidden) {
                brushShape.Visibility = Visibility.Visible;
                Canvas.SetLeft(brushShape, position.X - (_currentBrushSize / 2.0));
                Canvas.SetTop(brushShape, position.Y - (_currentBrushSize / 2.0));
                return;
            }
        }
    }
    
    private void HideBrushes() {
        foreach (var brushShapes in _brushShapes.Values) {
            foreach (var brushShape in brushShapes) {
                brushShape.Visibility = Visibility.Hidden;
            }
        }
    }

    private void ForestBrushButton_Click(object sender, RoutedEventArgs e) {
        _currentBrushType = BrushType.Forest;
    }

    private void MountainBrushButton_Click(object sender, RoutedEventArgs e) {
        _currentBrushType = BrushType.Mountain;
    }

    private void WaterBrushButton_Click(object sender, RoutedEventArgs e) {
        _currentBrushType = BrushType.Water;
    }

    private void ClearBrushButton_Click(object sender, RoutedEventArgs e) {
        _currentBrushType = BrushType.Clear;
    }

    private void MapCanvas_MouseDown(object sender, MouseButtonEventArgs e) {
        MapCanvas.CaptureMouse(); // Capture the mouse to receive MouseMove events
        MapCanvas.MouseMove += MapCanvas_MouseMove; // Subscribe to the MouseMove event
    }

    private void MapCanvas_MouseMove(object sender, MouseEventArgs e) {
        var position = e.GetPosition(MapCanvas); // Get the mouse position relative to the canvas
        UpdateBrushIndicator(position); // Update the brush indicator position
        DrawBrush(position, _currentBrushType); // Draw the brush
    }

    private void MapCanvas_MouseUp(object sender, MouseButtonEventArgs e) {
        MapCanvas.ReleaseMouseCapture(); // Stop capturing the mouse
        MapCanvas.MouseMove -= MapCanvas_MouseMove; // Unsubscribe from the MouseMove event
        // HideBrushes(); // Hide the brush shapes
    }

    private void BrushSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
        _currentBrushSize = (int)e.NewValue;
        MapCanvas.ReleaseMouseCapture(); // Release the mouse capture to stop receiving MouseMove events
        MapCanvas.MouseMove -= MapCanvas_MouseMove; // Unsubscribe from the MouseMove even
    }

}
