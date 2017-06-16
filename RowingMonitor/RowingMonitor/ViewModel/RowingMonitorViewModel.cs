using Microsoft.Kinect;
using RowingMonitor.Model.Pipeline;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Controls;

namespace RowingMonitor.ViewModel
{
    public class RowingMonitorViewModel
    {
        /* pipeline elemnts */
        // kinect reader
        private KinectReader kinectReader;

        // smoothing filter
        private SmoothingFilter smoothingFilter;
        private bool smoothingFilterChanged = false;

        // velocity smoothing filter
        private SmoothingFilter velocitySmoothingFilter;

        // origin shifter
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
        private VelocityCalculator velocityCalculator;
        private KleshnevVelocityCalculator kleshnevVelocityCalculator;

        // outputs
        private JointDataPlot jointDataPlot;

        /* GUI */
        private Frame frame1;

        // plot options
        private bool useKinectJointFilter = true;
        private bool useZVC = false;
        private float plotRange = 10;

        public RowingMonitorViewModel()
        {
            // create the pipeline elements
            kinectReader = KinectReader.Instance;
            smoothingFilter = new OneEuroSmoothingFilter(DataStreamType.SmoothedPosition);
            shifter = new Shifter();
            velocityCalculator = new VelocityCalculator();
            velocitySmoothingFilter = new OneEuroSmoothingFilter(DataStreamType.Velocity);
            segmentDetector = new ZVCSegmentDetector(minimumHitGap, startSegmentWithRisingVelocity);
            kleshnevVelocityCalculator = new KleshnevVelocityCalculator();
            jointDataPlot = new JointDataPlot(PlotRange);

            // link pipeline together
            kinectReader.JointDataBlock.LinkTo(smoothingFilter.SmoothingBlock);
            kinectReader.JointDataBlock.LinkTo(jointDataPlot.PlotJointDataBlock);

            smoothingFilter.SmoothingBlock.LinkTo(shifter.ShiftingBlock);
            smoothingFilter.SmoothingBlock.LinkTo(jointDataPlot.PlotJointDataBlock);

            shifter.ShiftingBlock.LinkTo(velocityCalculator.CalculationBlock);
            shifter.ShiftingBlock.LinkTo(jointDataPlot.PlotJointDataBlock);

            velocityCalculator.CalculationBlock.LinkTo(velocitySmoothingFilter.SmoothingBlock);

            velocitySmoothingFilter.SmoothingBlock.LinkTo(kleshnevVelocityCalculator.CalculationBlock);
            velocitySmoothingFilter.SmoothingBlock.LinkTo(segmentDetector.DetectionBlock);
            velocitySmoothingFilter.SmoothingBlock.LinkTo(jointDataPlot.PlotJointDataBlock);

            segmentDetector.DetectionBlock.LinkTo(jointDataPlot.PlotHitsBlock);            
        }

        public void WindowLoaded()
        {
            // create the GUI    
            Frame1.Content = jointDataPlot.View;

            // start the pipeline
            kinectReader.StartReader();
        }

        public List<JointType> PlotJointTypes {
            get => jointDataPlot.PlotJointTypes; set => jointDataPlot.PlotJointTypes = value; }
        public List<DataStreamType> PlotMeasuredVariables {
            get => jointDataPlot.PlotMeasuredVariables; set => jointDataPlot.PlotMeasuredVariables = value; }
        public bool UseKinectJointFilter { get => useKinectJointFilter; set => useKinectJointFilter = value; }
        public bool UseZVC { get => useZVC; set => useZVC = value; }
        public float PlotRange { get => plotRange; set => plotRange = value; }
        public Frame Frame1 { get => frame1; set => frame1 = value; }
    }
}
