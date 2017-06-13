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

        // DTW
        float distanceThreshold = 3.0f;
        int minimumSubsequenceLength = 10;

        // ZVC
        int minimumHitGap = 10;
        bool startSegmentWithRisingVelocity = true;

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
        private List<Util.DataStreamType> plotMeasuredVariables = new List<Util.DataStreamType>();
        private bool useKinectJointFilter = true;
        private bool useZVC = false;
        private float plotRange = 10;

        // plot buffer
        List<JointData> plotRawPositionBuffer = new List<JointData>();
        List<JointData> plotSmoothedPositionBuffer = new List<JointData>();
        List<JointData> plotVelocityBuffer = new List<JointData>();
        List<SegmentHit> hits = new List<SegmentHit>();

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

        // informations
        double startTimestamp;

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
            if (UseZVC) {
                segmentDetector = new ZVCSegmentDetector(minimumHitGap, startSegmentWithRisingVelocity);
            }
            else {
                segmentDetector = new DTWSegmentDetector(distanceThreshold, minimumSubsequenceLength);
            }
            segmentDetector.SegmentDetected += SegmentDetector_SegmentDetected;
            SegmentDetectorChanged = false;

            // init kleshnev analysis
            kleshnevVelocityCalculator = new KleshnevVelocityCalculator();
            kleshnevVelocityCalculator.KleshnevCalculationFinished +=
                KleshnevVelocityCalculator_KleshnevCalculationFinished;

            log.Info("################## ###### ###### ###### ##################");
        }

        /* Event Handler */
        void KinectReader_KinectFrameArrived(object sender, KinectFrameArrivedEventArgs e)
        {
            kinectJointFilter.UpdateFilter(e.JointData);

            plotRawPositionBuffer.Add(e.JointData);

            UpdateDefaultPlot();
            UpdateKleshnevPlots();

            startTimestamp = e.JointData.Timestamps[0];
        }

        private void KinectReader_ColorFrameArrivedAsync(object sender, Model.ColorFrameArrivedEventArgs e)
        {
            ColorBodyImageSource = e.ColorBitmap;
        }

        private void Filter_SmoothedFrameArrived(object sender, SmoothedFrameArrivedEventArgs e)
        {
            LogLatency("Filter", e.SmoothedJointData.Timestamps[0], e.SmoothedJointData.Timestamps);

            shifter.ShiftAndRotate(e.SmoothedJointData);

            // update frontal view skeleton
            frontalView.UpdateSkeleton(e.SmoothedJointData.Joints);
            FrontalBodyImageSource = frontalView.BodyImageSource;
        }

        private void Shifter_ShiftedFrameArrived(object sender,
            ShiftedFrameArrivedEventArgs e)
        {
            LogLatency("Shifter", e.ShiftedJointData.Timestamps[0], e.ShiftedJointData.Timestamps);

            // calculate velocites
            velCalc.CalculateVelocity(e.ShiftedJointData);

            plotSmoothedPositionBuffer.Add(e.ShiftedJointData);

            if (SegmentDetectorChanged) {
                segmentDetector.SegmentDetected -= SegmentDetector_SegmentDetected;
                if (UseZVC) {
                    segmentDetector = new ZVCSegmentDetector(minimumHitGap, startSegmentWithRisingVelocity);
                }
                else {
                    segmentDetector = new DTWSegmentDetector(distanceThreshold, minimumSubsequenceLength);
                }
                segmentDetector.SegmentDetected += SegmentDetector_SegmentDetected;
                SegmentDetectorChanged = false;
            }

            if (!UseZVC) {
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
            LogLatency("Velocity Calculator", e.CalculatedJointData.Timestamps[0], e.CalculatedJointData.Timestamps);

            velocityFilter.UpdateFilter(e.CalculatedJointData);
        }

        private void VelocityFilter_SmoothedFrameArrived(object sender, SmoothedFrameArrivedEventArgs e)
        {
            LogLatency("Velocity Filter", e.SmoothedJointData.Timestamps[0], e.SmoothedJointData.Timestamps);

            if (UseZVC) {
                segmentDetector.Update(e.SmoothedJointData,
                    JointType.HandRight, "Z");
            }

            kleshnevVelocityCalculator.CalculateKleshnevVelocities(
                e.SmoothedJointData);

            plotVelocityBuffer.Add(e.SmoothedJointData);
        }

        private void KleshnevVelocityCalculator_KleshnevCalculationFinished(
            object sender, KleshnevEventArgs e)
        {
            LogLatency("Kleshnev Velocity Calculator", startTimestamp);

            plotKlshBuffer = e.KleshnevData;
        }

        private void SegmentDetector_SegmentDetected(object sender, SegmentDetectedEventArgs e)
        {
            LogLatency("Segment Detector", startTimestamp);

            hits = e.Hits;
        }

        public void UpdateDefaultPlot()
        {
            Dictionary<string, List<PlotData>> plotData =
                new Dictionary<string, List<PlotData>>();
            foreach (DataStreamType measuredVariable in PlotMeasuredVariables) {
                switch (measuredVariable) {
                    case DataStreamType.RawPosition:
                        AddLinearPlotData(plotData, plotRawPositionBuffer, measuredVariable);
                        break;
                    case DataStreamType.SmoothedPosition:
                        AddLinearPlotData(plotData, plotSmoothedPositionBuffer, measuredVariable);
                        break;
                    case DataStreamType.Velocity:
                        AddLinearPlotData(plotData, plotVelocityBuffer, measuredVariable);
                        break;
                    case DataStreamType.SegmentHits:
                        List<PlotData> points = new List<PlotData>();
                        foreach (SegmentHit hit in hits) {
                            PlotData point = new PlotData();
                            point.X = hit.AbsTimestamp / 1000;
                            point.Annotation = hit.HitType.ToString();
                            point.DataStreamType = measuredVariable;
                            points.Add(point);
                        }
                        plotData.Add("Segment hits", points);
                        break;
                }
            }
            defaultPlot.UpdatePlot(plotData, "Values");
        }

        private void AddLinearPlotData(Dictionary<string, List<PlotData>> plotData, 
            List<JointData> jointDataBuffer, 
            DataStreamType dataStreamType)
        {
            foreach (JointType jointType in PlotJointTypes) {
                List<PlotData> rawPoints = new List<PlotData>();
                foreach (JointData jointData in jointDataBuffer) {
                    PlotData point = new PlotData();
                    point.X = jointData.AbsTimestamp / 1000;
                    point.Y = jointData.Joints[jointType].Position.Z;
                    point.DataStreamType = dataStreamType;
                    rawPoints.Add(point);
                }
                plotData.Add(dataStreamType.ToString() + " - " + jointType.ToString(), rawPoints);
            }
        }

        public void UpdateKleshnevPlots()
        {
            long[] segmentBounds = GetLastSegmentStartEnd(hits);
            if (segmentBounds != null) {
                long lastSegStartIndex = segmentBounds[0];
                long lastSegEndIndex = segmentBounds[1];

                // prepare disctionary
                Dictionary<string, List<PlotData>> lastPlotData = new Dictionary<string, List<PlotData>>();
                Dictionary<string, List<PlotData>> currentPlotData = new Dictionary<string, List<PlotData>>();                
                foreach (KleshnevVelocityType type in Enum.GetValues(typeof(KleshnevVelocityType))) {
                    lastPlotData.Add(type.ToString(), new List<PlotData>());
                    currentPlotData.Add(type.ToString(), new List<PlotData>());
                }

                foreach (KleshnevData klshData in plotKlshBuffer) {
                    if (klshData.Index >= lastSegStartIndex && klshData.Index <= lastSegEndIndex) {
                        foreach (KeyValuePair<KleshnevVelocityType, double> klshVel in klshData.Velocities) {
                            PlotData point = new PlotData();
                            point.X = klshData.AbsTimestamp / 1000;
                            point.Y = klshVel.Value;
                            point.DataStreamType = DataStreamType.Other;
                            lastPlotData[klshVel.Key.ToString()].Add(point);
                        }
                    }
                    else if (klshData.Index > lastSegEndIndex) {
                        foreach (KeyValuePair<KleshnevVelocityType, double> klshVel in klshData.Velocities) {
                            PlotData point = new PlotData();
                            point.X = klshData.AbsTimestamp / 1000;
                            point.Y = klshVel.Value;
                            currentPlotData[klshVel.Key.ToString()].Add(point);
                        }
                    }
                }

                klshLastSegmentPlot.UpdatePlot(lastPlotData, "Kleshnev Velocities Last Segment", klshColors);
                klshCurrentSegmentPlot.UpdatePlot(currentPlotData, "Kleshnev Velocity Current Segment", klshColors);

                LogExtractedSegmentTemplate(plotSmoothedPositionBuffer, lastSegStartIndex, lastSegEndIndex);
            }
        }

        private long[] GetLastSegmentStartEnd(List<SegmentHit> hits)
        {
            long[] segmentBounds = { -1, -1 };

            // segments can contain a start hit, an end hit, a end start hit and an internal hit
            // get the index for the last complete segment
            if (hits.Count >= 2) {
                for (int i = hits.Count - 1; i >= 0; i--) {
                    // find last end hit
                    if (segmentBounds[1] == -1 && (hits[i].HitType == HitType.SegmentEnd 
                        || hits[i].HitType == HitType.SegmentEndStart)) {
                        segmentBounds[1] = hits[i].Index;
                        continue;
                    }
                    // find last start hit after end hit was found
                    if (segmentBounds[1] != -1 && (hits[i].HitType == HitType.SegmentStart 
                        || hits[i].HitType == HitType.SegmentEndStart)) {
                        segmentBounds[0] = hits[i].Index;
                        return segmentBounds;
                    }
                }
            }
            
            return null;
        }

        private void LogLatency(string pipelineElement, double startTimestamp, List<double> timestamps = null)
        {
            double now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            log.Info(pipelineElement + " latency from start: " + (now - startTimestamp) + "ms");
            if (timestamps != null)
                log.Info(pipelineElement + " latency from last module: "
                    + (timestamps.Last() - timestamps[timestamps.Count - 2]) + "ms");
        }

        private void LogExtractedSegmentTemplate(List<JointData> jointDataBuffer, long segmentStart, long segmentEnd)
        {
            string template = "";
            foreach (JointData data in jointDataBuffer) {
                if (data.Index >= segmentStart && data.Index <= segmentEnd)
                template += data.Joints[JointType.HandRight].Position.Z + ";";
            }
            log.Info("Segment template: " + template);
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
        public List<Util.DataStreamType> PlotMeasuredVariables
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
