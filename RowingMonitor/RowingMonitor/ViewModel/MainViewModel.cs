﻿using System;
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

namespace RowingMonitor
{
    /// <summary>
    /// Represents the view-model for the main window.
    /// </summary>
    class MainViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;        

        // plot options
        private String selectedJointName;
        private JointType selectedJointType;

        // filter
        private OneEuroFilterSmoothing filter = new OneEuroFilterSmoothing();

        private KinectReader kinectReader;

        private FrontalView frontalView;

        private Plot plot;
        private Plot velPlot;

        private VelocityCalculator velCalc;
        

        /* Properties */
        /// <summary>
        /// Gets the plot model.
        /// </summary>
        public PlotModel Model {
            get {
                return plot.PlotModel;
            }
        }
        public PlotModel VelModel
        {
            get {
                return velPlot.PlotModel;
            }
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource BodyImageSource
        {
            get {
                return frontalView.BodyImageSource;
            }
        }

        public ImageSource ColorImageSource
        {
            get {
                return frontalView.ColorImageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get {
                return statusText;
            }

            set {
                if (statusText != value) {
                    statusText = value;

                    // notify any bound elements that the text has changed
                    if (PropertyChanged != null) {
                        PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        public double Beta {
            get => filter.Beta;
            set {
                filter.Beta = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Beta"));
            }
        }

        public double Fcmin {
            get => filter.Fcmin;
            set {
                filter.Fcmin = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Fcmin"));
            }
        }

        public string SelectedJointName { get => selectedJointName; set => selectedJointName = value; }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel" /> class.
        /// </summary>
        public MainViewModel()
        {
            kinectReader = KinectReader.Instance;

            frontalView = new FrontalView(kinectReader.CoordinateMapper, kinectReader.DisplayWidth, 
                kinectReader.DisplayHeight);

            plot = new Plot(200);
            velPlot = new Plot(200);

            // register event handler
            kinectReader.KinectFrameArrived += KinectReader_KinectFrameArrived;
            kinectReader.ColorFrameArrived += KinectReader_ColorFrameArrived;

            // register commands
            WindowLoaded = new RelayCommand(WindowLoadedCommand, CommandCanExecute);
            WindowClosing = new RelayCommand(WindowClosingCommand, CommandCanExecute);

            // set default plot option
            SelectedJointName = "SpineBase";

            // init filter
            filter.SmoothedFrameArrived += Filter_SmoothedFrameArrived;

            // init velocity calculation
            velCalc = new VelocityCalculator();
            velCalc.CalculatedFrameArrived += VelCalc_CalculatedFrameArrived;
        }

        private void VelCalc_CalculatedFrameArrived(object sender, CalculatedFrameArrivedEventArgs e)
        {
            // update plot
            int count = 0;
            Dictionary<String, List<Double[]>> dataPoints = new Dictionary<string, List<Double[]>>();
            dataPoints[SelectedJointName + " Velocity"] = new List<Double[]>();
            foreach (JointData jointData in e.KinectDataContainer.VelocityJointData) {
                Double[] values = new Double[2];
                values[0] = jointData.AbsTimestamp / 1000;
                values[1] = jointData.Joints[SelectedJointType].Position.Z;
                dataPoints[SelectedJointName + " Velocity"].Add(values);
                count++;
            }
            velPlot.Update(dataPoints, SelectedJointName + " Velocity Z");
            RaisePropertyChanged("VelModel");
        }

        private void KinectReader_ColorFrameArrived(object sender, Model.ColorFrameArrivedEventArgs e)
        {
            frontalView.UpdateColorImage(e.KinectDataContainer.ColorBitmap);
            RaisePropertyChanged("ColorImageSource");
        }

        private void Filter_SmoothedFrameArrived(object sender, SmoothedFrameArrivedEventArgs e)
        {
            // calculate velocites
            velCalc.CalculateVelocity(e.KinectDataContainer.SmoothedJointData);

            // update frontal view skeleton
            frontalView.UpdateSkeleton(e.KinectDataContainer.SmoothedJointData.Last().Joints);

            // update plot
            int count = 0;
            Dictionary<String, List<Double[]>> dataPoints = new Dictionary<string, List<Double[]>>();
            dataPoints[SelectedJointName + " Smoothed"] = new List<Double[]>();
            foreach (JointData jointData in e.KinectDataContainer.SmoothedJointData) {
                Double[] values = new Double[2];
                values[0] = jointData.AbsTimestamp / 1000;
                values[1] = jointData.Joints[SelectedJointType].Position.Z;
                dataPoints[SelectedJointName + " Smoothed"].Add(values);
                count++;
            }

            dataPoints[SelectedJointName + " Raw"] = new List<Double[]>();
            foreach (JointData jointData in e.KinectDataContainer.RawJointData) {
                Double[] values = new Double[2];
                values[0] = jointData.AbsTimestamp / 1000;
                values[1] = jointData.Joints[SelectedJointType].Position.Z;
                dataPoints[SelectedJointName + " Raw"].Add(values);
                count++;
            }
            plot.Update(dataPoints, SelectedJointName + " Z");
            RaisePropertyChanged("Model");
        }

        void KinectReader_KinectFrameArrived(object sender, KinectFrameArrivedEventArgs e)
        {
            filter.Filter();
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        private void WindowLoadedCommand(object obj)
        {
            kinectReader.StartReader();
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        private void WindowClosingCommand(object obj)
        {
            kinectReader.StopReader();
        }

        // need check for RelayCommand
        private bool CommandCanExecute(object obj)
        {
            return true;
        }

        // for realtime oxyplot
        protected void RaisePropertyChanged(string property)
        {
            var handler = this.PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }        
    }
}
