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
        private KinectJointFilter kinectJointFilter;

        private KinectReader kinectReader;

        private FrontalView frontalView;
        private SideView sideView;

        private Plot plot;
        private Plot velPlot;
        private Plot resultsPlot;

        private VelocityCalculator velCalc;

        private ImageSource bodyImageSource;

        private ImageSource sideBodyImageSource;

        private Timer timer;

        private Shifter shifter;

        private SegmentDetector segmentDetector;

        private KleshnevVelocityCalculator kleshnevVelocityCalculator;


        /* Properties */
        /// <summary>
        /// Gets the plot model.
        /// </summary>
        public PlotModel Model
        {
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
        public PlotModel ResultsModel
        {
            get {
                return resultsPlot.PlotModel;
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

        public ImageSource SideBodyImageSource
        {
            get => sideView.BodyImageSource;
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

        public double Beta
        {
            get => filter.Beta;
            set {
                filter.Beta = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Beta"));
            }
        }

        public double Fcmin
        {
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

            // skeleton views
            frontalView = new FrontalView(kinectReader.CoordinateMapper, kinectReader.DisplayWidth,
                kinectReader.DisplayHeight);
            sideView = new SideView(kinectReader.CoordinateMapper, kinectReader.DisplayWidth,
                kinectReader.DisplayHeight);

            plot = new Plot(10);
            velPlot = new Plot(10);
            resultsPlot = new Plot(3);

            // register event handler
            kinectReader.KinectFrameArrived += KinectReader_KinectFrameArrived;
            kinectReader.ColorFrameArrived += KinectReader_ColorFrameArrivedAsync;

            // register commands
            WindowLoaded = new RelayCommand(WindowLoadedCommand, CommandCanExecute);
            WindowClosing = new RelayCommand(WindowClosingCommand, CommandCanExecute);

            // set default plot option
            SelectedJointName = "SpineBase";

            // init filter
            //filter.SmoothedFrameArrived += Filter_SmoothedFrameArrivedAsync;
            kinectJointFilter = new KinectJointFilter();
            //kinectJointFilter.Init();   // suggested value
            // Some smoothing with little latency (defaults).
            // Only filters out small jitters.
            // Good for gesture recognition in games.
            //kinectJointFilter.Init(0.5f, 0.5f, 0.5f, 0.05f, 0.04f);
            // Smoothed with some latency.
            // Filters out medium jitters.
            // Good for a menu system that needs to be smooth but
            // doesn't need the reduced latency as much as gesture recognition does.
            kinectJointFilter.Init(0.5f, 0.1f, 0.5f, 0.1f, 0.1f);
            // Very smooth, but with a lot of latency.
            // Filters out large jitters.
            // Good for situations where smooth data is absolutely required
            // and latency is not an issue.
            //kinectJointFilter.Init(0.7f, 0.3f, 1.0f, 1.0f, 1.0f);
            kinectJointFilter.SmoothedFrameArrived += Filter_SmoothedFrameArrivedAsync;

            // init shifter
            shifter = new Shifter();
            shifter.ShiftedFrameArrived += Shifter_ShiftedFrameArrivedAsync;

            // init velocity calculation
            velCalc = new VelocityCalculator();
            velCalc.CalculatedFrameArrived += VelCalc_CalculatedFrameArrivedAsync;

            // init segment detector
            segmentDetector = new SegmentDetector();
            segmentDetector.SegmentDetected += SegmentDetector_SegmentDetected;

            // init kleshnev analysis
            kleshnevVelocityCalculator = new KleshnevVelocityCalculator();
            kleshnevVelocityCalculator.KleshnevCalculationFinished += KleshnevVelocityCalculator_KleshnevCalculationFinished;

            // start render loop
            // timer = new Timer(Render, null, 0, 33);
        }        

        private void Render(object state)
        {
            RaisePropertyChanged("VelModel");
            RaisePropertyChanged("ColorImageSource");
            RaisePropertyChanged("BodyImageSource");
            RaisePropertyChanged("SideBodyImageSource");
            RaisePropertyChanged("Model");
            RaisePropertyChanged("ResultsModel");
        }

        /* Event Handler */
        void KinectReader_KinectFrameArrived(object sender, KinectFrameArrivedEventArgs e)
        {
            Task.Run(() => {
                //filter.Filter();
                kinectJointFilter.UpdateFilter(e.JointData);
            });            
        }

        private void KinectReader_ColorFrameArrivedAsync(object sender, Model.ColorFrameArrivedEventArgs e)
        {
            frontalView.UpdateColorImageAsync(e.ColorBitmap);
            //RaisePropertyChanged("ColorImageSource");
        }

        private void Filter_SmoothedFrameArrivedAsync(object sender, SmoothedFrameArrivedEventArgs e)
        {
            //update frontal view skeleton
            frontalView.UpdateSkeletonAsync(e.SmoothedJointData.Joints);
            //RaisePropertyChanged("BodyImageSource");

            //update plot
            //int count = 0;
            //Dictionary<String, List<Double[]>> dataPoints = new Dictionary<string, List<Double[]>>();
            //dataPoints[SelectedJointName + " Smoothed"] = new List<Double[]>();
            //foreach (JointData jointData in e.SmoothedJointData) {
            //    Double[] values = new Double[2];
            //    values[0] = jointData.AbsTimestamp / 1000;
            //    values[1] = jointData.Joints[SelectedJointType].Position.Z;
            //    dataPoints[SelectedJointName + " Smoothed"].Add(values);
            //    count++;
            //}

            //dataPoints[SelectedJointName + " Raw"] = new List<Double[]>();
            //foreach (JointData jointData in e.RawJointData) {
            //    Double[] values = new Double[2];
            //    values[0] = jointData.AbsTimestamp / 1000;
            //    values[1] = jointData.Joints[SelectedJointType].Position.Z;
            //    dataPoints[SelectedJointName + " Raw"].Add(values);
            //    count++;
            //}
            //plot.UpdatePlot(dataPoints, SelectedJointName + " Z");
            //RaisePropertyChanged("Model");

            shifter.ShiftAndRotate(e.SmoothedJointData);
        }

        private void Shifter_ShiftedFrameArrivedAsync(object sender, 
            ShiftedFrameArrivedEventArgs e)
        {
            // calculate velocites
            //velCalc.CalculateVelocity(e.KinectDataContainer.ShiftedJointData);
            velCalc.CalculateVelocity(e.ShiftedJointData);

            // show side view
            sideView.UpdateSkeletonAsync(e.ShiftedJointData.Joints);
        }       

        private void VelCalc_CalculatedFrameArrivedAsync(object sender,
            CalculatedFrameArrivedEventArgs e)
        {
            // check for segments
            segmentDetector.SegmentByZeroCrossings(e.CalculatedJointData, 
                JointType.HandRight, "Z");

            // calculate Kleshnev
            kleshnevVelocityCalculator.CalculateKleshnevVelocities(e.CalculatedJointData);

            // update plot
            //int count = 0;
            //Dictionary<String, List<Double[]>> dataPoints = new Dictionary<string, List<Double[]>>();
            //dataPoints[SelectedJointName + " Velocity"] = new List<Double[]>();
            //dataPoints["Hits"] = new List<double[]>();
            //foreach (JointData jointData in e.CalculatedJointData) {
            //    Double[] values = new Double[2];
            //    values[0] = jointData.AbsTimestamp / 1000;
            //    values[1] = jointData.Joints[SelectedJointType].Position.Z;
            //    dataPoints[SelectedJointName + " Velocity"].Add(values);

            //    // check if index is hit
            //    if (e.KinectDataContainer.Hits.Contains(jointData.Index)) {
            //        Double[] hit = new Double[2];
            //        hit[0] = jointData.AbsTimestamp / 1000;
            //        hit[1] = 0;
            //        dataPoints["Hits"].Add(hit);
            //    }

            //    count++;
            //}
            //velPlot.UpdatePlot(dataPoints, SelectedJointName + " Velocity Z");
            //RaisePropertyChanged("VelModel");
        }

        private void KleshnevVelocityCalculator_KleshnevCalculationFinished(
            object sender, KleshnevEventArgs e)
        {
            //Dictionary<String, OxyColor> colors = new Dictionary<string, OxyColor>();
            //colors.Add(KleshnevVelocityType.ArmsLeft.ToString(), OxyColors.LightGreen);
            //colors.Add(KleshnevVelocityType.ArmsRight.ToString(), OxyColors.Green);
            //colors.Add(KleshnevVelocityType.HandleLeft.ToString(), OxyColors.Gray);
            //colors.Add(KleshnevVelocityType.HandleRight.ToString(), OxyColors.Black);
            //colors.Add(KleshnevVelocityType.Legs.ToString(), OxyColors.Red);
            //colors.Add(KleshnevVelocityType.Trunk.ToString(), OxyColors.Blue);

            //Dictionary<String, List<Double[]>> dataPoints = new Dictionary<string, List<Double[]>>();
            //foreach (KleshnevVelocityType type in Enum.GetValues(typeof(KleshnevVelocityType))) {
            //    dataPoints[type.ToString()] = new List<Double[]>();
            //}
            //foreach (KleshnevData kleshnevData in e.KleshnevData) {
            //    foreach (KeyValuePair<KleshnevVelocityType, double> velocity in kleshnevData.Velocities) {
            //        Double[] values = new Double[2];
            //        values[0] = kleshnevData.AbsTimestamp / 1000;
            //        values[1] = velocity.Value;
            //        dataPoints[velocity.Key.ToString()].Add(values);
            //    }
            //}
            //resultsPlot.UpdatePlot(dataPoints, "Kleshnev Velocities", colors);
        }

        private void SegmentDetector_SegmentDetected(object sender, SegmentDetectedEventArgs e)
        {
            Debug.WriteLine("*** SEGMENT DETECTED! ***");
        }

        /* UI Event Handler */
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
            kinectJointFilter.Shutdown();
        }

        // need check for RelayCommand
        private bool CommandCanExecute(object obj)
        {
            return true;
        }

        // for realtime oxyplot
        protected void RaisePropertyChanged(string property)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
