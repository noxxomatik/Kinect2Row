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

namespace RowingMonitor.ViewModel
{
    public class JointDataPlotViewModel : INotifyPropertyChanged
    {
        private PlotModel plotModel;

        public event PropertyChangedEventHandler PropertyChanged;

        public JointDataPlotViewModel(){}     
        
        public void Update(PlotModel plotModel)
        {
            Application.Current?.Dispatcher.Invoke(new Action(() =>
            {
                PlotModel = plotModel;
            }));            
        }

        /// <summary>
        /// Represents the plot.
        /// </summary>
        public PlotModel PlotModel
        {
            get => plotModel;
            set {
                plotModel = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlotModel"));
            }
        }
    }
}
