using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LiveCharts;
using LiveCharts.Defaults;
using MFFSuCA.Models;
using MFFSuCA.Utility;

namespace MFFSuCA.Windows;

/// <summary>
/// Interaction logic for SimulationChartsWindow.xaml
/// </summary>
public partial class SimulationChartsWindow {
    /// <summary>
    /// Raw data from the simulation.
    /// </summary>
    public SimulationData Data { get; init; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SimulationChartsWindow"/> class.
    /// </summary>
    public SimulationChartsWindow() {
        try {
            InitializeComponent();
            DataContext = this;
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while initializing the simulation charts window." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Data for the Burning Area chart.
    /// </summary>
    public ChartValues<ObservableValue> BurningAreaSeriesCollection { get; } = new();

    /// <summary>
    /// Data for the Burned Area chart.
    /// </summary>
    public ChartValues<ObservableValue> BurnedAreaSeriesCollection { get; } = new();

    /// <summary>
    /// Data for the Burning Speed chart.
    /// </summary>
    public ChartValues<ObservableValue> BurningSpeedSeriesCollection { get; } = new();

    /// <summary>
    /// Data for the Damaged Vegetation chart.
    /// </summary>
    public ChartValues<ObservableValue> DamagedVegetationSeriesCollection { get; } = new();

    /// <summary>
    /// Updates the charts with the data from <see cref="Data"/>.
    /// </summary>
    public void UpdateCharts() {
        try {
            BurningAreaSeriesCollection.Clear();
            foreach (var data in Data.BurningArea) {
                BurningAreaSeriesCollection.Add(new ObservableValue(data));
            }

            BurnedAreaSeriesCollection.Clear();
            foreach (var data in Data.BurnedArea) {
                BurnedAreaSeriesCollection.Add(new ObservableValue(data));
            }

            BurningSpeedSeriesCollection.Clear();
            foreach (var data in Data.BurningSpeed) {
                BurningSpeedSeriesCollection.Add(new ObservableValue(data));
            }

            DamagedVegetationSeriesCollection.Clear();
            foreach (var data in Data.DamagedVegetation) {
                DamagedVegetationSeriesCollection.Add(new ObservableValue(data));
            }
        }
        catch (Exception exc) {
            Logger.Log(exc);
        }
    }

    /// <summary>
    /// Exports the charts to .png and .jpeg files.
    /// </summary>
    /// <param name="fileName">Name of the file.</param>
    private void ExportCharts(string fileName) {
        try {
            var renderTarget = new RenderTargetBitmap((int)ChartsGrid.ActualWidth, (int)ChartsGrid.ActualHeight,
                                                      96, 96, PixelFormats.Default);
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
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while exporting the charts." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Saves all the data and charts to results folder.
    /// </summary>
    public void SaveAll() {
        try {
            UpdateCharts();

            if (!Directory.Exists("results"))
                Directory.CreateDirectory("results");

            var dir = $"results/{DateTime.Now:dd-MM-yyyy_HH-mm-ss}";
            Directory.CreateDirectory(dir);
            ExportCharts($"{dir}/charts");
            SaveData(dir);
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while saving the results." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Saves the <see cref="Data"/> to a .csv file.
    /// </summary>
    /// <param name="dir">The directory.</param>
    private void SaveData(string dir) {
        try {
            var csv = new StringBuilder();

            csv.AppendLine("Time;BurningArea;BurnedArea;BurningSpeed;DamagedVegetation");
            for (var i = 0; i < Data.BurningArea.Count; i++) {
                csv.AppendLine($"{i};{Data.BurningArea[i]};{Data.BurnedArea[i]};{Data.BurningSpeed[i]};{Data.DamagedVegetation[i]}");
            }

            using var fileStream = new FileStream($"{dir}/data.csv", FileMode.Create);
            fileStream.Write(Encoding.UTF8.GetBytes(csv.ToString()));
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while saving the data." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
