using System.Linq;
using System.Windows;

namespace MFFSuCA.Windows; 

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow {
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow() {
        InitializeComponent();
    }

    /// <summary>
    /// Opens the map constructor window.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void MapConstructorButton_Click(object sender, RoutedEventArgs e) {
        if (Application.Current.Windows.OfType<MapConstructorWindow>().Any()) {
            Application.Current.Windows.OfType<MapConstructorWindow>().First().Activate();
            return;
        }
        var mapConstructorWindow = new MapConstructorWindow();
        mapConstructorWindow.Show();
        Close();
    }

    /// <summary>
    /// Opens the simulation window.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void SimulationButton_Click(object sender, RoutedEventArgs e) {
        if (Application.Current.Windows.OfType<SimulationWindow>().Any()) {
            Application.Current.Windows.OfType<SimulationWindow>().First().Activate();
            return;
        }
        var simulationWindow = new SimulationWindow();
        simulationWindow.Show();
        Close();
    }
}
