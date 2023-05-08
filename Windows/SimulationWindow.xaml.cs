using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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
    
    private readonly Dictionary<Brush, BrushType> _brushTypes = new() {
        { new SolidColorBrush(Colors.DarkGreen), BrushType.Forest },
        { new SolidColorBrush(Colors.Green), BrushType.Grassland },
        { new SolidColorBrush(Colors.PaleGreen), BrushType.Plain },
        { new SolidColorBrush(Colors.SaddleBrown), BrushType.Mountain },
        { new SolidColorBrush(Colors.Blue), BrushType.Water },
        { new SolidColorBrush(Colors.DimGray), BrushType.HighDensityUrbanArea },
        { new SolidColorBrush(Colors.DarkGray), BrushType.LowDensityUrbanArea },
        { new SolidColorBrush(Colors.White), BrushType.Clear }
    };

    private Dictionary<Rectangle, Cell> _cells = new();

    private CellularAutomaton _automaton = new CellularAutomaton();

    public SimulationWindow() {
        InitializeComponent();
    }

    private void InitializeCells() {
        _cells = new();
        foreach (var row in _rectArray) {
            foreach (var rect in row) {
                var cell = new Cell {
                    TerrainType = _brushTypes[(SolidColorBrush)rect.Fill]
                };
                _cells.Add(rect, cell);
            }
        }
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
}