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

        private ImageSource bodyImageSource;

        private Timer timer;

        private Shifter shifter;


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
            kinectReader.ColorFrameArrived += KinectReader_ColorFrameArrivedAsync;

            // register commands
            WindowLoaded = new RelayCommand(WindowLoadedCommand, CommandCanExecute);
            WindowClosing = new RelayCommand(WindowClosingCommand, CommandCanExecute);

            // set default plot option
            SelectedJointName = "SpineBase";

            // init filter
            filter.SmoothedFrameArrived += Filter_SmoothedFrameArrivedAsync;

            // init shifter
            shifter = new Shifter();
            shifter.ShiftedFrameArrived += Shifter_ShiftedFrameArrivedAsync;

            // init velocity calculation
            velCalc = new VelocityCalculator();
            velCalc.CalculatedFrameArrived += VelCalc_CalculatedFrameArrivedAsync;

            // start render loop
            timer = new Timer(Render, null, 0, 33);
        }

        private void Render(object state)
        {
            RaisePropertyChanged("VelModel");
            RaisePropertyChanged("ColorImageSource");
            RaisePropertyChanged("BodyImageSource");
            RaisePropertyChanged("Model");
        }

        private async void Shifter_ShiftedFrameArrivedAsync(object sender, ShiftedFrameArrivedEventArgs e)
        {
            // calculate velocites
            velCalc.CalculateVelocity(e.KinectDataContainer.ShiftedJointData);
        }

        private async void VelCalc_CalculatedFrameArrivedAsync(object sender, CalculatedFrameArrivedEventArgs e)
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
            await velPlot.UpdateAsync(dataPoints, SelectedJointName + " Velocity Z");
            //RaisePropertyChanged("VelModel");
        }

        private async void KinectReader_ColorFrameArrivedAsync(object sender, Model.ColorFrameArrivedEventArgs e)
        {
            await frontalView.UpdateColorImageAsync(e.KinectDataContainer.ColorBitmap);
            //RaisePropertyChanged("ColorImageSource");
        }

        private async void Filter_SmoothedFrameArrivedAsync(object sender, SmoothedFrameArrivedEventArgs e)
        {
            // update frontal view skeleton
            frontalView.UpdateSkeletonAsync(e.KinectDataContainer.SmoothedJointData.Last().Joints);
            //RaisePropertyChanged("BodyImageSource");

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
            await plot.UpdateAsync(dataPoints, SelectedJointName + " Z");
            //RaisePropertyChanged("Model");

            shifter.ShiftAndRotate(e.KinectDataContainer.SmoothedJointData.Last());            
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
