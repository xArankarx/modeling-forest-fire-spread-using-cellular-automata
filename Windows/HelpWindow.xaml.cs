using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using MFFSuCA.Utility;

namespace MFFSuCA.Windows; 

/// <summary>
/// Interaction logic for HelpWindow.xaml
/// </summary>
public partial class HelpWindow {
    /// <summary>
    /// Initializes a new instance of the <see cref="HelpWindow"/> class.
    /// </summary>
    public HelpWindow() {
        try {
            InitializeComponent();
        }
        catch (Exception exc) {
            Logger.Log(exc);
            MessageBox.Show("An error occurred while initializing the help window." +
                            " See error_log.txt for more information.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Handles the OnRequestNavigate event of the Hyperlink control.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e) {
        try {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) {
                UseShellExecute = true
            });
        } catch (Exception exc) {
            Logger.Log(exc);
        }
    }
}
