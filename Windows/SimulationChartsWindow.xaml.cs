using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using MFFSuCA.Models;

namespace MFFSuCA.Windows; 

public partial class SimulationChartsWindow {
    public SimulationData Data { get; init; } = new();

        public SimulationChartsWindow()
        {
            InitializeComponent();
            DataContext = this;
            // InitializeCharts();
        }

        public ChartValues<ObservableValue> BurningAreaSeriesCollection { get; set; } = new();

        public ChartValues<ObservableValue> BurnedAreaSeriesCollection { get; set; } = new();

        public ChartValues<ObservableValue> BurningSpeedSeriesCollection { get; set; } = new();

        public ChartValues<ObservableValue> DamagedVegetationSeriesCollection { get; set; } = new();

        // private void InitializeCharts()
        // {
        //     // initialize BurningArea chart
        //     var burningAreaSeries = new LineSeries { Title = "Burning Area" };
        //     BurningAreaSeriesCollection.Add(burningAreaSeries);
        //     BurningAreaSeriesCollection[0].Values = new ChartValues<ObservableValue>();
        //
        //     // initialize BurnedArea chart
        //     var burnedAreaSeries = new LineSeries { Title = "Burned Area" };
        //     BurnedAreaSeriesCollection.Add(burnedAreaSeries);
        //     BurnedAreaSeriesCollection[0].Values = new ChartValues<ObservableValue>();
        //
        //     // initialize BurningSpeed chart
        //     var burningSpeedSeries = new LineSeries { Title = "Burning Speed" };
        //     BurningSpeedSeriesCollection.Add(burningSpeedSeries);
        //     BurningSpeedSeriesCollection[0].Values = new ChartValues<ObservableValue>();
        //
        //     // initialize DamagedVegetation chart
        //     var damagedVegetationSeries = new LineSeries { Title = "Damaged Vegetation" };
        //     DamagedVegetationSeriesCollection.Add(damagedVegetationSeries);
        //     DamagedVegetationSeriesCollection[0].Values = new ChartValues<ObservableValue>();
        // }

        public void UpdateCharts()
        {
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
}
