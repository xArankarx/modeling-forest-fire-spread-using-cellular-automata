using System;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LiveCharts;
using LiveCharts.Defaults;
using MFFSuCA.Models;
// ReSharper disable MemberCanBePrivate.Global

namespace MFFSuCA.Windows;

public partial class SimulationChartsWindow {
    public SimulationData Data { get; init; } = new();

    public SimulationChartsWindow() {
        InitializeComponent();
        DataContext = this;
    }

    public ChartValues<ObservableValue> BurningAreaSeriesCollection { get; } = new();

    public ChartValues<ObservableValue> BurnedAreaSeriesCollection { get; } = new();

    public ChartValues<ObservableValue> BurningSpeedSeriesCollection { get; } = new();

    public ChartValues<ObservableValue> DamagedVegetationSeriesCollection { get; } = new();

    public void UpdateCharts() {
        // update BurningArea chart
        BurningAreaSeriesCollection.Clear();
        foreach (var data in Data.BurningArea) {
            BurningAreaSeriesCollection.Add(new ObservableValue(data));
        }

        // update BurnedArea chart
        BurnedAreaSeriesCollection.Clear();
        foreach (var data in Data.BurnedArea) {
            BurnedAreaSeriesCollection.Add(new ObservableValue(data));
        }

        // update BurningSpeed chart
        BurningSpeedSeriesCollection.Clear();
        foreach (var data in Data.BurningSpeed) {
            BurningSpeedSeriesCollection.Add(new ObservableValue(data));
        }

        // update DamagedVegetation chart
        DamagedVegetationSeriesCollection.Clear();
        foreach (var data in Data.DamagedVegetation) {
            DamagedVegetationSeriesCollection.Add(new ObservableValue(data));
        }
    }

    private void ExportCharts(string fileName) {
        var renderTarget =
            new RenderTargetBitmap((int)ChartsGrid.ActualWidth, (int)ChartsGrid.ActualHeight, 96, 96, PixelFormats.Default);
        renderTarget.Render(ChartsGrid);
        var pngEncoder = new PngBitmapEncoder();
        pngEncoder.Frames.Add(BitmapFrame.Create(renderTarget));
        using var fileStream = new FileStream(fileName + ".png", FileMode.Create);
        pngEncoder.Save(fileStream);
        var jpegEncoder = new JpegBitmapEncoder();
        jpegEncoder.Frames.Add(BitmapFrame.Create(renderTarget));
        using var fileStream2 = new FileStream(fileName + ".jpeg", FileMode.Create);
        jpegEncoder.Save(fileStream2);
    }

    public void SaveAll() {
        UpdateCharts();
        if (!Directory.Exists("results"))
            Directory.CreateDirectory("results");
        var dir = $"results/{DateTime.Now:dd-MM-yyyy_HH-mm-ss}";
        Directory.CreateDirectory(dir);
        ExportCharts($"{dir}/charts");
        SaveData(dir);
    }

    private void SaveData(string dir) {
        var csv = new StringBuilder();
        csv.AppendLine("Time;BurningArea;BurnedArea;BurningSpeed;DamagedVegetation");
        for (var i = 0; i < Data.BurningArea.Count; i++) {
            csv.AppendLine($"{i};{Data.BurningArea[i]};{Data.BurnedArea[i]};{Data.BurningSpeed[i]};{Data.DamagedVegetation[i]}");
        }
        using var fileStream = new FileStream($"{dir}/data.csv", FileMode.Create);
        fileStream.Write(Encoding.UTF8.GetBytes(csv.ToString()));
    }
}
