using OxyPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RowingMonitor.ViewModel
{
    class KleshnevPlotViewModel : INotifyPropertyChanged
    {
        private PlotModel lastSegmentPlotModel;
        private PlotModel currentSegmentPlotModel;

        public event PropertyChangedEventHandler PropertyChanged;

        public KleshnevPlotViewModel() { }

        public void UpdateLastSegmentPlot(PlotModel lastSegmentPlot)
        {
            Application.Current?.Dispatcher.Invoke(new Action(() =>
            {
                LastSegmentPlotModel = lastSegmentPlot;
            }));
        }

        public void UpdateCurrentSegmentPlot(PlotModel currentSegmentPlot)
        {
            Application.Current?.Dispatcher.Invoke(new Action(() =>
            {
                CurrentSegmentPlotModel = currentSegmentPlot;
            }));
        }

        public PlotModel LastSegmentPlotModel
        {
            get => lastSegmentPlotModel; set {
                lastSegmentPlotModel = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LastSegmentPlotModel"));
            }
        }
        public PlotModel CurrentSegmentPlotModel
        {
            get => currentSegmentPlotModel; set {
                currentSegmentPlotModel = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentSegmentPlotModel"));
            }
        }
    }
}
