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
using Sensorit.Base;
using RowingMonitor.Model;

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

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public ICommand WindowLoaded { get; private set; }
        public ICommand WindowClosing { get; private set; }

        // plot options
        private String selectedJointName;
        private JointType selectedJointType;

        // filter
        //private OneEuroFilter filter;
        private OneEuroFilterSmoothing filter = new OneEuroFilterSmoothing();

        private KinectReader kinectReader;

        private FrontalView frontalView;

        private Plot plot;

        /* Properties */
        /// <summary>
        /// Gets the plot model.
        /// </summary>
        public PlotModel Model {
            get {
                return plot.PlotModel;
            }
        }

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get {
                return frontalView.ImageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get {
                return this.statusText;
            }

            set {
                if (this.statusText != value) {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null) {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        public double Beta { get => filter.Beta; set => filter.Beta = value; }
        public double Fcmin { get => filter.Fcmin; set => filter.Fcmin = value; }
        public string SelectedJointName { get => selectedJointName; set => selectedJointName = value; }
        public JointType SelectedJointType
        {
            get {
                switch (this.SelectedJointName) {
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
        /// Initializes a new instance of the <see cref="MainViewModel" /> class.
        /// </summary>
        public MainViewModel()
        {
            kinectReader = KinectReader.Instance;

            frontalView = new FrontalView(kinectReader.CoordinateMapper, kinectReader.DisplayWidth, 
                kinectReader.DisplayHeight);

            plot = new Plot(200);

            // register event handler
            kinectReader.KinectFrameArrived += KinectReader_KinectFrameArrived;

            // register commands
            this.WindowLoaded = new RelayCommand(WindowLoadedCommand, CommandCanExecute);
            this.WindowClosing = new RelayCommand(WindowClosingCommand, CommandCanExecute);

            // set default 1€ filter values
            this.Beta = 0.0001f;
            this.Fcmin = 1;

            // set default plot option
            this.SelectedJointName = "SpineBase";

            // init filter
            //filter = new OneEuroFilterSmoothing();
            filter.SmoothedFrameArrived += Filter_SmoothedFrameArrived;
        }

        private void Filter_SmoothedFrameArrived(object sender, SmoothedFrameArrivedEventArgs e)
        {
            // update frontal view
            frontalView.Update(e.KinectDataContainer.Bodies);

            // update plot
            int count = 0;
            Dictionary<String, List<Double[]>> dataPoints = new Dictionary<string, List<Double[]>>();
            dataPoints[SelectedJointName.ToString()] = new List<Double[]>();
            foreach (JointData jointData in e.KinectDataContainer.SmoothedJointData) {
                Double[] values = new Double[2];
                values[0] = jointData.AbsTimestamp / 1000;
                // TODO: bodies[1] is the one that is tracked? check is tracked
                values[1] = jointData.Joints[SelectedJointType].Position.Z;
                dataPoints[SelectedJointName].Add(values);
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
