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
        // kinect reader
        private KinectReader kinectReader;

        // smoothing filter
        private OneEuroFilterSmoothing oneEuroFilterSmoothing =
            new OneEuroFilterSmoothing();
        private KinectJointFilter kinectJointFilter;

        // velocity smoothing filter
        private KinectJointFilter velocityFilter;

        // shift
        private Shifter shifter;

        // segmentation
        private SegmentDetector segmentDetector;
        private bool segmentDetectorChanged;

        // calculations
        private VelocityCalculator velCalc;
        private KleshnevVelocityCalculator kleshnevVelocityCalculator;

        // skeleton views
        private FrontalView frontalView;
        private SideView sideView;

        // plot
        private Plot defaultPlot;
        // plot options
        private List<JointType> plotJointTypes = new List<JointType>();
        private List<PlotOptionsMeasuredVariables> plotMeasuredVariables = new List<PlotOptionsMeasuredVariables>();
        private bool useKinectJointFilter = true;
        private bool useZVC = false;
        private float plotRange = 10;

        // plot buffer
        List<JointData> plotRawPositionBuffer = new List<JointData>();
        List<JointData> plotSmoothedPositionBuffer = new List<JointData>();
        List<JointData> plotVelocityBuffer = new List<JointData>();
        List<double> segmentHitsTimestamps = new List<double>();
        List<long> segmentHitsIndices = new List<long>();

        // Kleshnev Plots
        private Plot klshLastSegmentPlot;
        private Plot klshCurrentSegmentPlot;
        private PlotModel klshLastSegmentPlotModel;
        private PlotModel klshCurrentSegmentPlotModel;
        List<KleshnevData> plotKlshBuffer = new List<KleshnevData>();

        private Dictionary<String, OxyColor> klshColors;

        // image sources
        private ImageSource frontalBodyImageSource;
        private ImageSource sideBodyImageSource;
        private ImageSource colorBodyImageSource;

        // Logger
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

            // init plots
            defaultPlot = new Plot(PlotRange);
            defaultPlot.Init("Values");

            klshColors = new Dictionary<string, OxyColor>();
            klshColors.Add(KleshnevVelocityType.ArmsLeft.ToString(), OxyColors.LightGreen);
            klshColors.Add(KleshnevVelocityType.ArmsRight.ToString(), OxyColors.Green);
            klshColors.Add(KleshnevVelocityType.HandleLeft.ToString(), OxyColors.Gray);
            klshColors.Add(KleshnevVelocityType.HandleRight.ToString(), OxyColors.Black);
            klshColors.Add(KleshnevVelocityType.Legs.ToString(), OxyColors.Red);
            klshColors.Add(KleshnevVelocityType.Trunk.ToString(), OxyColors.Blue);

            klshLastSegmentPlot = new Plot();
            klshLastSegmentPlot.Init("Kleshnev Velocities Last Segment", klshColors);

            klshCurrentSegmentPlot = new Plot(3);
            klshCurrentSegmentPlot.Init("Kleshnev Velocities Current Segment", klshColors);

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

            velocityFilter = new KinectJointFilter();
            velocityFilter.Init(0.7f, 0.3f, 1.0f, 1.0f, 1.0f);
            velocityFilter.SmoothedFrameArrived += VelocityFilter_SmoothedFrameArrived;

            // init shifter
            shifter = new Shifter();
            shifter.ShiftedFrameArrived += Shifter_ShiftedFrameArrived;

            // init velocity calculation
            velCalc = new VelocityCalculator();
            velCalc.CalculatedFrameArrived += VelCalc_CalculatedFrameArrivedAsync;

            // init segment detector
            if (UseZVC)
            {
                segmentDetector = new ZVCSegmentDetector();
            }
            else
            {
                segmentDetector = new DTWSegmentDetector();
            }
            segmentDetector.SegmentDetected += SegmentDetector_SegmentDetected;
            SegmentDetectorChanged = false;

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

            plotRawPositionBuffer.Add(e.JointData);

            UpdateDefaultPlot();
            UpdateKleshnevPlots();
        }

        private void KinectReader_ColorFrameArrivedAsync(object sender, Model.ColorFrameArrivedEventArgs e)
        {
            ColorBodyImageSource = e.ColorBitmap;
        }

        private void Filter_SmoothedFrameArrived(object sender, SmoothedFrameArrivedEventArgs e)
        {
            shifter.ShiftAndRotate(e.SmoothedJointData);

            // update frontal view skeleton
            frontalView.UpdateSkeleton(e.SmoothedJointData.Joints);
            FrontalBodyImageSource = frontalView.BodyImageSource;
        }

        private void Shifter_ShiftedFrameArrived(object sender,
            ShiftedFrameArrivedEventArgs e)
        {
            // calculate velocites
            velCalc.CalculateVelocity(e.ShiftedJointData);

            plotSmoothedPositionBuffer.Add(e.ShiftedJointData);
            log.Info(e.ShiftedJointData.Joints[JointType.HandRight].Position.Z);

            if (SegmentDetectorChanged)
            {
                segmentDetector.SegmentDetected -= SegmentDetector_SegmentDetected;
                if (UseZVC)
                {
                    segmentDetector = new ZVCSegmentDetector();
                }
                else
                {
                    segmentDetector = new DTWSegmentDetector();
                }
                segmentDetector.SegmentDetected += SegmentDetector_SegmentDetected;
                SegmentDetectorChanged = false;
            }

            if (!UseZVC)
            {
                segmentDetector.Update(e.ShiftedJointData,
                    JointType.HandRight, "Z");
            }

            // show side view
            sideView.UpdateSkeleton(e.ShiftedJointData.Joints);
            SideBodyImageSource = sideView.BodyImageSource;

            // sonificate the position
            Task.Run(() =>
            {
                System.Console.Beep(2000 + (int)(1000 * e.ShiftedJointData.Joints[JointType.HandRight].Position.Z), 30);
            });
        }

        private void VelCalc_CalculatedFrameArrivedAsync(object sender,
            CalculatedFrameArrivedEventArgs e)
        {
            velocityFilter.UpdateFilter(e.CalculatedJointData);
        }

        private void VelocityFilter_SmoothedFrameArrived(object sender, SmoothedFrameArrivedEventArgs e)
        {
            if (UseZVC)
            {
                segmentDetector.Update(e.SmoothedJointData,
                    JointType.HandRight, "Z");
            }

            kleshnevVelocityCalculator.CalculateKleshnevVelocities(
                e.SmoothedJointData);

            plotVelocityBuffer.Add(e.SmoothedJointData);


            log.Info("Latency: " +
                (e.SmoothedJointData.Timestamps.Last()
                - e.SmoothedJointData.Timestamps[0]));
        }

        private void KleshnevVelocityCalculator_KleshnevCalculationFinished(
            object sender, KleshnevEventArgs e)
        {
            plotKlshBuffer = e.KleshnevData;
        }

        private void SegmentDetector_SegmentDetected(object sender, SegmentDetectedEventArgs e)
        {
            segmentHitsIndices = e.HitIndices;
            segmentHitsTimestamps = e.HitTimestamps;
        }

        public void UpdateDefaultPlot()
        {
            Dictionary<string, List<double[]>> plotData =
                new Dictionary<string, List<double[]>>();
            foreach (PlotOptionsMeasuredVariables measuredVariable in PlotMeasuredVariables)
            {
                switch (measuredVariable)
                {
                    case PlotOptionsMeasuredVariables.RawPosition:
                        foreach (JointType jointType in PlotJointTypes)
                        {
                            List<double[]> rawPoints = new List<double[]>();
                            foreach (JointData jointData in plotRawPositionBuffer)
                            {
                                double[] point = new double[2];
                                point[0] = jointData.AbsTimestamp / 1000;
                                point[1] = jointData.Joints[jointType].Position.Z;
                                rawPoints.Add(point);
                            }
                            plotData.Add("Raw position - " + jointType.ToString(), rawPoints);
                        }
                        break;
                    case PlotOptionsMeasuredVariables.SmoothedPosition:
                        foreach (JointType jointType in PlotJointTypes)
                        {
                            List<double[]> smoPoints = new List<double[]>();
                            foreach (JointData jointData in plotSmoothedPositionBuffer)
                            {
                                double[] point = new double[2];
                                point[0] = jointData.AbsTimestamp / 1000;
                                point[1] = jointData.Joints[jointType].Position.Z;
                                smoPoints.Add(point);
                            }
                            plotData.Add("Smoothed position - " + jointType.ToString(), smoPoints);
                        }
                        break;
                    case PlotOptionsMeasuredVariables.Velocity:
                        foreach (JointType jointType in PlotJointTypes)
                        {
                            List<double[]> valPoints = new List<double[]>();
                            foreach (JointData jointData in plotVelocityBuffer)
                            {
                                double[] point = new double[2];
                                point[0] = jointData.AbsTimestamp / 1000;
                                point[1] = jointData.Joints[jointType].Position.Z;
                                valPoints.Add(point);
                            }
                            plotData.Add("Velocity - " + jointType.ToString(), valPoints);
                        }
                        break;
                    case PlotOptionsMeasuredVariables.SegmentHits:
                        List<double[]> points = new List<double[]>();
                        foreach (double timestamp in segmentHitsTimestamps)
                        {
                            double[] point = new double[2];
                            point[0] = timestamp / 1000;
                            point[1] = UseZVC? 0 : 1;
                            points.Add(point);
                        }
                        plotData.Add("Segment hits", points);
                        break;
                }
            }
            defaultPlot.UpdatePlot(plotData, "Values");
        }

        public void UpdateKleshnevPlots()
        {
            if (segmentHitsTimestamps.Count > 2)
            {
                long lastSegStartIndex = segmentHitsIndices[segmentHitsIndices.Count - 2];
                long lastSegEndIndex = segmentHitsIndices.Last();

                Dictionary<string, List<double[]>> lastPlotData = new Dictionary<string, List<double[]>>();
                Dictionary<string, List<double[]>> currentPlotData = new Dictionary<string, List<double[]>>();
                // prepare disctionary
                foreach (KleshnevVelocityType type in Enum.GetValues(typeof(KleshnevVelocityType)))
                {
                    lastPlotData.Add(type.ToString(), new List<double[]>());
                    currentPlotData.Add(type.ToString(), new List<double[]>());
                }
                foreach (KleshnevData klshData in plotKlshBuffer)
                {
                    if (klshData.Index >= lastSegStartIndex && klshData.Index <= lastSegEndIndex)
                    {
                        foreach (KeyValuePair<KleshnevVelocityType, double> klshVel in klshData.Velocities)
                        {
                            double[] point = new double[2];
                            point[0] = klshData.AbsTimestamp / 1000;
                            point[1] = klshVel.Value;
                            lastPlotData[klshVel.Key.ToString()].Add(point);
                        }
                    }
                    else if (klshData.Index > lastSegEndIndex)
                    {
                        foreach (KeyValuePair<KleshnevVelocityType, double> klshVel in klshData.Velocities)
                        {
                            double[] point = new double[2];
                            point[0] = klshData.AbsTimestamp / 1000;
                            point[1] = klshVel.Value;
                            currentPlotData[klshVel.Key.ToString()].Add(point);
                        }
                    }
                }
                klshLastSegmentPlot.UpdatePlot(lastPlotData, "Kleshnev Velocities Last Segment", klshColors);
                klshCurrentSegmentPlot.UpdatePlot(currentPlotData, "Kleshnev Velocity Current Segment", klshColors);
            }
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

        public ImageSource FrontalBodyImageSource
        {
            get => frontalBodyImageSource; set => frontalBodyImageSource = value;
        }
        public ImageSource SideBodyImageSource
        {
            get => sideBodyImageSource; set => sideBodyImageSource = value;
        }
        public ImageSource ColorBodyImageSource
        {
            get => colorBodyImageSource; set => colorBodyImageSource = value;
        }
        public PlotModel DefaultPlotModel
        {
            get => defaultPlot.PlotModel;
        }
        public List<JointType> PlotJointTypes
        {
            get => plotJointTypes; set => plotJointTypes = value;
        }
        public List<PlotOptionsMeasuredVariables> PlotMeasuredVariables
        {
            get => plotMeasuredVariables; set => plotMeasuredVariables = value;
        }
        public bool UseKinectJointFilter
        {
            get => useKinectJointFilter; set => useKinectJointFilter = value;
        }
        public bool UseZVC
        {
            get => useZVC; set {
                useZVC = value;
                SegmentDetectorChanged = true;
            }
        }
        public PlotModel KlshLastSegmentPlotModel
        {
            get => klshLastSegmentPlot.PlotModel;
        }
        public PlotModel KlshCurrentSegmentPlotModel
        {
            get => klshCurrentSegmentPlot.PlotModel;
        }
        public float PlotRange { get => plotRange; set => plotRange = value; }
        public bool SegmentDetectorChanged { get => segmentDetectorChanged; set => segmentDetectorChanged = value; }
    }
}
