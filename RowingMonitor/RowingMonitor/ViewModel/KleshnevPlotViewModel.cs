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

        public void Render()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LastSegmentPlotModel"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentSegmentPlotModel"));
        }

        public void UpdateLastSegmentPlot(PlotModel lastSegmentPlot)
        {
            LastSegmentPlotModel = lastSegmentPlot;
        }

        public void UpdateCurrentSegmentPlot(PlotModel currentSegmentPlot)
        {
            CurrentSegmentPlotModel = currentSegmentPlot;
        }

        public PlotModel LastSegmentPlotModel
        {
            get => lastSegmentPlotModel; set {
                lastSegmentPlotModel = value;
            }
        }
        public PlotModel CurrentSegmentPlotModel
        {
            get => currentSegmentPlotModel; set {
                currentSegmentPlotModel = value;
            }
        }
    }
}
