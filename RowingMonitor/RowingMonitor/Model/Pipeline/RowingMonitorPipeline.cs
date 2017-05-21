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
using RowingMonitor.ViewModel;

namespace RowingMonitor.Model.Pipeline
{
    public class RowingMonitorPipeline
    {
        // plot options
        private String selectedJointName;
        private JointType selectedJointType;

        // kinect reader
        private KinectReader kinectReader;

        // smoothing filter
        private OneEuroFilterSmoothing oneEuroFilterSmoothing =
            new OneEuroFilterSmoothing();
        private KinectJointFilter kinectJointFilter;

        // shift
        private Shifter shifter;

        // segmentation
        private SegmentDetector segmentDetector;

        // calculations
        private VelocityCalculator velCalc;
        private KleshnevVelocityCalculator kleshnevVelocityCalculator;

        // skeleton views
        private FrontalView frontalView;
        private SideView sideView;

        // plots
        private Plot positionPlot;
        private Plot velocityPlot;
        private Plot kleshnevPlot;

        // image sources
        private ImageSource frontalBodyImageSource;
        private ImageSource sideBodyImageSource;
        private ImageSource colorBodyImageSource;

        private Timer timer;

        public RowingMonitorPipeline()
        {
            kinectReader = KinectReader.Instance;

            // skeleton views
            frontalView = new FrontalView(kinectReader.CoordinateMapper, 
                kinectReader.DisplayWidth,
                kinectReader.DisplayHeight);
            sideView = new SideView(kinectReader.CoordinateMapper, 
                kinectReader.DisplayWidth,
                kinectReader.DisplayHeight);

            PositionPlot = new Plot(10);
            PositionPlot.Init("Raw and Smoothed Curves");
            VelocityPlot = new Plot(10);
            VelocityPlot.Init("Velocity");
            KleshnevPlot = new Plot(3);
            Dictionary<String, OxyColor> colors = new Dictionary<string, OxyColor>();
            colors.Add(KleshnevVelocityType.ArmsLeft.ToString(), OxyColors.LightGreen);
            colors.Add(KleshnevVelocityType.ArmsRight.ToString(), OxyColors.Green);
            colors.Add(KleshnevVelocityType.HandleLeft.ToString(), OxyColors.Gray);
            colors.Add(KleshnevVelocityType.HandleRight.ToString(), OxyColors.Black);
            colors.Add(KleshnevVelocityType.Legs.ToString(), OxyColors.Red);
            colors.Add(KleshnevVelocityType.Trunk.ToString(), OxyColors.Blue);
            KleshnevPlot.Init("Kleshnev Velocities", colors);

            // register event handler
            kinectReader.KinectFrameArrived += KinectReader_KinectFrameArrived;
            kinectReader.ColorFrameArrived += KinectReader_ColorFrameArrivedAsync;

            // init filter
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
            kinectJointFilter.SmoothedFrameArrived += Filter_SmoothedFrameArrived;

            // init shifter
            shifter = new Shifter();
            shifter.ShiftedFrameArrived += Shifter_ShiftedFrameArrived;

            // init velocity calculation
            velCalc = new VelocityCalculator();
            velCalc.CalculatedFrameArrived += VelCalc_CalculatedFrameArrivedAsync;

            // init segment detector
            segmentDetector = new SegmentDetector();
            segmentDetector.SegmentDetected += SegmentDetector_SegmentDetected;

            // init kleshnev analysis
            kleshnevVelocityCalculator = new KleshnevVelocityCalculator();
            kleshnevVelocityCalculator.KleshnevCalculationFinished += 
                KleshnevVelocityCalculator_KleshnevCalculationFinished;
        }

        /* Event Handler */
        void KinectReader_KinectFrameArrived(object sender, KinectFrameArrivedEventArgs e)
        {
            //Task.Run(() => {
                kinectJointFilter.UpdateFilter(e.JointData);
            //});
        }

        private void KinectReader_ColorFrameArrivedAsync(object sender, Model.ColorFrameArrivedEventArgs e)
        {
            ColorBodyImageSource = e.ColorBitmap;
        }

        private void Filter_SmoothedFrameArrived(object sender, SmoothedFrameArrivedEventArgs e)
        {
            //Task.Run(() => {
                shifter.ShiftAndRotate(e.SmoothedJointData);
            //});

            // update frontal view skeleton
            frontalView.UpdateSkeleton(e.SmoothedJointData.Joints);
            FrontalBodyImageSource = frontalView.BodyImageSource;

            // update plot
            Double[] rawValues = new Double[2];
            rawValues[0] = e.RawJointData.AbsTimestamp / 1000;
            rawValues[1] = e.RawJointData.Joints[SelectedJointType].Position.Z;
            PositionPlot.AddDataPoint(SelectedJointName + " Raw", rawValues);

            Double[] smoothedValues = new Double[2];
            smoothedValues[0] = e.SmoothedJointData.AbsTimestamp / 1000;
            smoothedValues[1] = e.SmoothedJointData.Joints[SelectedJointType].Position.Z;
            PositionPlot.AddDataPoint(SelectedJointName + " Smoothed", smoothedValues);
        }

        private void Shifter_ShiftedFrameArrived(object sender,
            ShiftedFrameArrivedEventArgs e)
        {
            //Task.Run(() => {
                // calculate velocites
                velCalc.CalculateVelocity(e.ShiftedJointData);
            //});

            // show side view
            sideView.UpdateSkeleton(e.ShiftedJointData.Joints);
            SideBodyImageSource = sideView.BodyImageSource;
        }

        private void VelCalc_CalculatedFrameArrivedAsync(object sender,
            CalculatedFrameArrivedEventArgs e)
        {
            //Task.Run(() => {
                // check for segments
                segmentDetector.SegmentByZeroCrossings(e.CalculatedJointData,
                    JointType.HandRight, "Z");
            //});
            //Task.Run(() => {
                // calculate Kleshnev
                kleshnevVelocityCalculator.CalculateKleshnevVelocities(
                e.CalculatedJointData);
            //});

            // update plot
            Double[] velocityValues = new Double[2];
            velocityValues[0] = e.CalculatedJointData.AbsTimestamp / 1000;
            velocityValues[1] = e.CalculatedJointData.Joints[SelectedJointType].Position.Z;
            VelocityPlot.AddDataPoint(SelectedJointName + " Velocity", velocityValues);

            Debug.WriteLine("Latency: " +
                (e.CalculatedJointData.Timestamps.Last()
                - e.CalculatedJointData.Timestamps[0]));
        }

        private void KleshnevVelocityCalculator_KleshnevCalculationFinished(
            object sender, KleshnevEventArgs e)
        {
            foreach (KeyValuePair<KleshnevVelocityType, double> velocity in e.KleshnevData.Last().Velocities) {
                Double[] values = new Double[2];
                values[0] = e.KleshnevData.Last().AbsTimestamp / 1000;
                values[1] = velocity.Value;
                KleshnevPlot.AddDataPoint(velocity.Key.ToString(), values);
            }
        }

        private void SegmentDetector_SegmentDetected(object sender, SegmentDetectedEventArgs e)
        {
            Double[] hitValues = new Double[2];
            hitValues[0] = e.HitTimestamps.Last() / 1000;
            hitValues[1] = 0;
            VelocityPlot.AddDataPoint(SelectedJointName + " Hits", hitValues);
        }

        public void StartPipeline()
        {
            kinectReader.StartReader();
        }

        public void StopPipeline()
        {
            kinectReader.StopReader();
            kinectJointFilter.Shutdown();
        }

        public string SelectedJointName
        {
            get => selectedJointName;
            set => selectedJointName = value;
        }

        public JointType SelectedJointType
        {
            get => selectedJointType;
            set => selectedJointType = value;
        }

        public ImageSource FrontalBodyImageSource { get => frontalBodyImageSource; set => frontalBodyImageSource = value; }
        public ImageSource SideBodyImageSource { get => sideBodyImageSource; set => sideBodyImageSource = value; }
        public ImageSource ColorBodyImageSource { get => colorBodyImageSource; set => colorBodyImageSource = value; }
        public Plot PositionPlot { get => positionPlot; set => positionPlot = value; }
        public Plot VelocityPlot { get => velocityPlot; set => velocityPlot = value; }
        public Plot KleshnevPlot { get => kleshnevPlot; set => kleshnevPlot = value; }
    }
}
