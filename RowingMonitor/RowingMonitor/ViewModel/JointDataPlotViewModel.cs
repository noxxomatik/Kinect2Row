using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using System.Threading.Tasks.Dataflow;
using RowingMonitor.Model.Util;
using System.ComponentModel;
using System.Windows;
using System.Threading;
using System.Windows.Controls;

namespace RowingMonitor.ViewModel
{
    public class JointDataPlotViewModel : INotifyPropertyChanged
    {
        private PlotModel plotModel;
        private PlotModel plotModelBuffer;

        public event PropertyChangedEventHandler PropertyChanged;

        private Timer timer;

        public JointDataPlotViewModel() { }

        public void Render()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlotModel"));
        }

        public void Update(PlotModel plotModel)
        {
            PlotModel = plotModel;
        }

        /// <summary>
        /// Represents the plot.
        /// </summary>
        public PlotModel PlotModel
        {
            get => plotModel;
            set {
                plotModel = value;
            }
        }
    }
}
