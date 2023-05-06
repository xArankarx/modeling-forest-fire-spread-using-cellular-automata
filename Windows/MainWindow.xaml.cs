using System.Windows;

namespace MFFSuCA.Windows {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            var mapConstructorWindow = new MapConstructorWindow();
            mapConstructorWindow.Show();
        }
    }
}