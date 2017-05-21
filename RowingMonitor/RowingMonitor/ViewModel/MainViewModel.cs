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

        // plot options
        private String selectedJointName;
        private JointType selectedJointType;

        private Timer timer;

        private PlotModel model;
        private PlotModel velModel;
        private PlotModel resultsModel;

        private ImageSource bodyImageSource;
        private ImageSource colorImageSource;
        private ImageSource sideBodyImageSource;

        private double beta;
        private double fcmin;

        public string SelectedJointName
        {
            get => selectedJointName;
            set {
                selectedJointName = value;
                pipeline.SelectedJointName = selectedJointName;
            }
        }

        public JointType SelectedJointType
        {
            get {
                switch (SelectedJointName) {
                    case "SpineBase":
                        return JointType.SpineBase;
                    case "HandRight":
                        return JointType.HandRight;
                    case "HandLeft":
                        return JointType.HandLeft;
                    default:
                        return JointType.SpineBase;
                }
            }
            set => selectedJointType = value;
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand WindowLoaded { get; private set; }
        public ICommand WindowClosing { get; private set; }
        public PlotModel Model { get => model; set => model = value; }
        public PlotModel VelModel { get => velModel; set => velModel = value; }
        public PlotModel ResultsModel { get => resultsModel; set => resultsModel = value; }
        public ImageSource BodyImageSource { get => bodyImageSource; set => bodyImageSource = value; }
        public ImageSource SideBodyImageSource { get => sideBodyImageSource; set => sideBodyImageSource = value; }
        public ImageSource ColorImageSource { get => colorImageSource; set => colorImageSource = value; }
        public double Beta { get => beta; set => beta = value; }
        public double Fcmin { get => fcmin; set => fcmin = value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel" /> class.
        /// </summary>
        public MainViewModel()
        {
            pipeline = new RowingMonitorPipeline();
            Model = pipeline.PositionPlot.PlotModel;
            VelModel = pipeline.VelocityPlot.PlotModel;
            ResultsModel = pipeline.KleshnevPlot.PlotModel;

            ColorImageSource = pipeline.ColorBodyImageSource;
            BodyImageSource = pipeline.FrontalBodyImageSource;
            SideBodyImageSource = pipeline.SideBodyImageSource;

            // register commands
            WindowLoaded = new RelayCommand(WindowLoadedCommand, CommandCanExecute);
            WindowClosing = new RelayCommand(WindowClosingCommand, CommandCanExecute);

            // set default plot option
            SelectedJointName = "SpineBase";

            // start render loop
            timer = new Timer(Render, null, 0, 33);
        }

        private void Render(object state)
        {
            //RaisePropertyChanged("ColorImageSource");
            //RaisePropertyChanged("BodyImageSource");
            //RaisePropertyChanged("SideBodyImageSource");
            //RaisePropertyChanged("Model");
            //RaisePropertyChanged("VelModel");
            //RaisePropertyChanged("ResultsModel");

            ColorImageSource = pipeline.ColorBodyImageSource;
            if (ColorImageSource != null)
                RaisePropertyChanged("ColorImageSource");

            BodyImageSource = pipeline.FrontalBodyImageSource;
            if (BodyImageSource != null)
                RaisePropertyChanged("BodyImageSource");

            SideBodyImageSource = pipeline.SideBodyImageSource;
            if (SideBodyImageSource != null)
                RaisePropertyChanged("SideBodyImageSource");
            
            //if (Model != null)
            //    Model.InvalidatePlot(true);

            //if (VelModel != null)
            //    VelModel.InvalidatePlot(true);

            //if (ResultsModel != null)
            //    ResultsModel.InvalidatePlot(true);
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
    }
}
