using System;
using System.Linq;
using System.Windows;
using MFFSuCA.Utility;

namespace MFFSuCA.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow {
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow() {
        try {
            InitializeComponent();
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while initializing the main window." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Opens the map constructor window.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void MapConstructorButton_Click(object sender, RoutedEventArgs e) {
        try {
            if (Application.Current.Windows.OfType<MapConstructorWindow>().Any()) {
                Application.Current.Windows.OfType<MapConstructorWindow>().First().Activate();
                return;
            }

            var mapConstructorWindow = new MapConstructorWindow();
            mapConstructorWindow.Show();
            Close();
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while opening the map constructor window." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Opens the simulation window.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void SimulationButton_Click(object sender, RoutedEventArgs e) {
        try {
            if (Application.Current.Windows.OfType<SimulationWindow>().Any()) {
                Application.Current.Windows.OfType<SimulationWindow>().First().Activate();
                return;
            }

            var simulationWindow = new SimulationWindow();
            simulationWindow.Show();
            Close();
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while opening the simulation window." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the click event of the Help menu item.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void AboutMenuItem_Click(object sender, RoutedEventArgs e) {
        try {
            if (Application.Current.Windows.OfType<HelpWindow>().Any()) {
                Application.Current.Windows.OfType<HelpWindow>().First().Activate();
                return;
            }

            var aboutWindow = new HelpWindow();
            aboutWindow.Show();
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while opening the about window." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
