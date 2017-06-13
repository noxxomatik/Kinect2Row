using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OxyPlot;
using OxyPlot.Series;
using Microsoft.Kinect;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;
using RowingMonitor.Model;
using Xceed.Wpf.Toolkit;
using System.Windows.Controls;
using System.Threading;
using RowingMonitor.Model.Pipeline;
using System.Diagnostics;
using RowingMonitor.Model.Util;

namespace RowingMonitor.ViewModel
{
    /// <summary>
    /// Represents the view-model for the main window.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private RowingMonitorPipeline pipeline;

        private Timer timer;

        private ImageSource bodyImageSource;
        private ImageSource colorImageSource;
        private ImageSource sideBodyImageSource;

        private double beta;
        private double fcmin;

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;        

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel" /> class.
        /// </summary>
        public MainViewModel()
        {
            pipeline = new RowingMonitorPipeline();

            ColorImageSource = pipeline.ColorBodyImageSource;
            BodyImageSource = pipeline.FrontalBodyImageSource;
            SideBodyImageSource = pipeline.SideBodyImageSource;

            // register commands
            WindowLoaded = new RelayCommand(WindowLoadedCommand, CommandCanExecute);
            WindowClosing = new RelayCommand(WindowClosingCommand, CommandCanExecute);            

            // start render loop
            timer = new Timer(Render, null, 0, 33);
        }

        private void Render(object state)
        {
            ColorImageSource = pipeline.ColorBodyImageSource;
            if (ColorImageSource != null)
                RaisePropertyChanged("ColorImageSource");

            BodyImageSource = pipeline.FrontalBodyImageSource;
            if (BodyImageSource != null)
                RaisePropertyChanged("BodyImageSource");

            SideBodyImageSource = pipeline.SideBodyImageSource;
            if (SideBodyImageSource != null)
                RaisePropertyChanged("SideBodyImageSource");

            RaisePropertyChanged("DefaultPlotModel");
            RaisePropertyChanged("KlshLastSegmentPlotModel");
            RaisePropertyChanged("KlshCurrentSegmentPlotModel");
        }

        /* UI Event Handler */
        /// <summary>
        /// Execute start up tasks
        /// </summary>
        private void WindowLoadedCommand(object obj)
        {
            pipeline.StartPipeline();
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        private void WindowClosingCommand(object obj)
        {
            pipeline.StopPipeline();
        }

        // need check for RelayCommand
        private bool CommandCanExecute(object obj)
        {
            return true;
        }

        protected void RaisePropertyChanged(string property)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public ICommand WindowLoaded { get; private set; }
        public ICommand WindowClosing { get; private set; }
        public ImageSource BodyImageSource
        {
            get => bodyImageSource; set => bodyImageSource = value;
        }
        public ImageSource SideBodyImageSource
        {
            get => sideBodyImageSource; set => sideBodyImageSource = value;
        }
        public ImageSource ColorImageSource
        {
            get => colorImageSource; set => colorImageSource = value;
        }
        public double Beta
        {
            get => beta; set => beta = value;
        }
        public double Fcmin
        {
            get => fcmin; set => fcmin = value;
        }
        public List<JointType> PlotJointTypes
        {
            get => pipeline.PlotJointTypes; set => pipeline.PlotJointTypes = value;
        }
        public List<Model.Util.DataStreamType> PlotMeasuredVariables
        {
            get => pipeline.PlotMeasuredVariables; set => pipeline.PlotMeasuredVariables = value;
        }
        public bool UseKinectJointFilter
        {
            get => pipeline.UseKinectJointFilter; set => pipeline.UseKinectJointFilter = value;
        }
        public bool UseZVC
        {
            get => pipeline.UseZVC; set => pipeline.UseZVC = value;
        }
        public PlotModel DefaultPlotModel
        {
            get => pipeline.DefaultPlotModel;
        }
        public PlotModel KlshLastSegmentPlotModel
        {
            get => pipeline.KlshLastSegmentPlotModel;
        }
        public PlotModel KlshCurrentSegmentPlotModel
        {
            get => pipeline.KlshCurrentSegmentPlotModel;
        }
    }
}
