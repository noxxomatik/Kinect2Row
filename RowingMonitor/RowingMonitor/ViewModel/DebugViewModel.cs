﻿using Microsoft.Kinect;
using RowingMonitor.Model.Pipeline;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Controls;

namespace RowingMonitor.ViewModel
{
    public class DebugViewModel
    {
        /* pipeline elemnts */
        // kinect reader
        private KinectReader kinectReader;

        // smoothing filter
        private SmoothingFilter smoothingFilter;
        // 1€ filter parameter
        private double beta = 0;
        private double fcmin = 2;

        // velocity smoothing filter
        private SmoothingFilter velocitySmoothingFilter;

        // origin shifter
        private Shifter shifter;

        // segmentation
        private SegmentDetector segmentDetector;
        // DTW
        float distanceThreshold = 2.0f;
        int minimumSubsequenceLength = 10;
        // ZVC
        int minimumHitGap = 10;
        bool startSegmentWithRisingVelocity = true;

        // calculations
        private VelocityCalculator velocityCalculator;
        private KleshnevVelocityCalculator kleshnevVelocityCalculator;

        // meta data
        private RowingMetadataCalculator metaDataCalculator;

        // outputs
        private JointDataPlot jointDataPlot;
        private SkeletonFrontalDisplay skeletonFrontalDisplay;
        private SkeletonSideDisplay skeletonSideDisplay;
        private KleshnevPlot kleshnevPlot;
        private TrunkAngleDisplay trunkAngleDisplay;
        private RowingMetadataDebugDisplay metaDataDisplay;
        private RowingSonification rowingSonification;
        private RowingMetadataWidgetsDisplay widgetsDisplay;

        /* GUI */
        private Grid grid;

        // plot options
        private bool useKinectJointFilter = true;
        private bool useZVC = false;
        private float plotRange = 10;
        private float kleshnevPlotRange = 5;

        // remember links to dispose them
        private IDisposable segmentationLink;
        private IDisposable smoothingLink;
        private IDisposable velocitySmoothingLink;
        private IDisposable readerLinkPlot;
        private IDisposable readerLinkSkeleton;
        private IDisposable readerLinkColor;

        // render timer
        private Timer timer;

        public DebugViewModel()
        {
            // create the pipeline elements
            kinectReader = KinectReader.Instance;
            //smoothingFilter = new OneEuroSmoothingFilter(DataStreamType.SmoothedPosition);
            shifter = new Shifter();
            velocityCalculator = new VelocityCalculator();
            //velocitySmoothingFilter = new OneEuroSmoothingFilter(DataStreamType.Velocity);
            segmentDetector = new ZVCSegmentDetector(minimumHitGap, startSegmentWithRisingVelocity);
            kleshnevVelocityCalculator = new KleshnevVelocityCalculator();
            metaDataCalculator = new RowingMetadataCalculator();

            jointDataPlot = new JointDataPlot(PlotRange);
            skeletonFrontalDisplay = new SkeletonFrontalDisplay(
                kinectReader.CoordinateMapper, kinectReader.DepthFrameDescription,
                kinectReader.ColorFrameDescription);
            skeletonSideDisplay = new SkeletonSideDisplay();
            kleshnevPlot = new KleshnevPlot(KleshnevPlotRange);
            trunkAngleDisplay = new TrunkAngleDisplay();
            metaDataDisplay = new RowingMetadataDebugDisplay();
            rowingSonification = new RowingSonification();
            widgetsDisplay = new RowingMetadataWidgetsDisplay();

            // link pipeline together
            //kinectReader.JointDataBlock.LinkTo(smoothingFilter.SmoothingBlock);
            readerLinkPlot = kinectReader.JointDataBlock.LinkTo(jointDataPlot.PlotJointDataBlock);
            readerLinkSkeleton = kinectReader.JointDataBlock.LinkTo(skeletonFrontalDisplay.SkeletonBlock);
            readerLinkColor = kinectReader.ColorFrameBlock.LinkTo(skeletonFrontalDisplay.ColorImageBlock);

            //smoothingFilter.SmoothingBlock.LinkTo(shifter.ShiftingBlock);
            //smoothingFilter.SmoothingBlock.LinkTo(jointDataPlot.PlotJointDataBlock);

            shifter.Output.LinkTo(velocityCalculator.Input);
            shifter.Output.LinkTo(jointDataPlot.PlotJointDataBlock);
            shifter.Output.LinkTo(skeletonSideDisplay.SkeletonBlock);
            shifter.Output.LinkTo(trunkAngleDisplay.Input);
            shifter.Output.LinkTo(metaDataCalculator.Input);

            //velocityCalculator.CalculationBlock.LinkTo(
            //    velocitySmoothingFilter.SmoothingBlock);

            //velocitySmoothingFilter.SmoothingBlock.LinkTo(kleshnevVelocityCalculator.CalculationBlock);
            ////velocitySmoothingFilter.SmoothingBlock.LinkTo(segmentDetector.DetectionInputBlock);
            //velocitySmoothingFilter.SmoothingBlock.LinkTo(jointDataPlot.PlotJointDataBlock);

            kleshnevVelocityCalculator.Output.LinkTo(kleshnevPlot.KleshnevDataBlock);
            kleshnevVelocityCalculator.Output.LinkTo(metaDataCalculator.InputKleshnevData);
            kleshnevVelocityCalculator.Output.LinkTo(rowingSonification.Input);

            //segmentDetector.DetectionOutputBlock.LinkTo(jointDataPlot.PlotHitsBlock);
            //segmentDetector.DetectionOutputBlock.LinkTo(kleshnevPlot.PlotHitsBlock);

            metaDataCalculator.Output.LinkTo(metaDataDisplay.Input);
            metaDataCalculator.Output.LinkTo(widgetsDisplay.Input);
        }

        private void Render(object state)
        {
            jointDataPlot.Render();
            kleshnevPlot.Render();
            skeletonSideDisplay.Render();
            skeletonFrontalDisplay.Render();
            trunkAngleDisplay.Render();
            metaDataDisplay.Render();
            widgetsDisplay.Render();
        }

        public void ViewLoaded()
        {
            // create the GUI    
            Frame plotFrame = new Frame();
            plotFrame.Content = jointDataPlot.View;
            plotFrame.SetValue(Grid.RowProperty, 1);
            plotFrame.SetValue(Grid.ColumnProperty, 1);
            Grid.Children.Add(plotFrame);

            Frame frontalDisplayFrame = new Frame();
            frontalDisplayFrame.Content = skeletonFrontalDisplay.View;
            frontalDisplayFrame.SetValue(Grid.RowProperty, 0);
            frontalDisplayFrame.SetValue(Grid.ColumnProperty, 1);
            Grid.Children.Add(frontalDisplayFrame);

            Frame sideDisplayFrame = new Frame();
            sideDisplayFrame.Content = skeletonSideDisplay.View;
            sideDisplayFrame.SetValue(Grid.RowProperty, 0);
            sideDisplayFrame.SetValue(Grid.ColumnProperty, 2);
            Grid.Children.Add(sideDisplayFrame);

            Frame kleshnevPlotFrame = new Frame();
            kleshnevPlotFrame.Content = kleshnevPlot.View;
            kleshnevPlotFrame.SetValue(Grid.RowProperty, 2);
            kleshnevPlotFrame.SetValue(Grid.ColumnProperty, 1);
            kleshnevPlotFrame.SetValue(Grid.ColumnSpanProperty, 2);
            kleshnevPlotFrame.SetValue(Grid.RowSpanProperty, 2);
            Grid.Children.Add(kleshnevPlotFrame);

            //Frame trunkAngleFrame = new Frame();
            //trunkAngleFrame.Content = trunkAngleDisplay.View;
            //trunkAngleFrame.SetValue(Grid.RowProperty, 1);
            //trunkAngleFrame.SetValue(Grid.ColumnProperty, 2);
            //Grid.Children.Add(trunkAngleFrame);

            Frame metaDataFrame = new Frame();
            metaDataFrame.Content = metaDataDisplay.View;
            metaDataFrame.SetValue(Grid.RowProperty, 0);
            metaDataFrame.SetValue(Grid.ColumnProperty, 0);
            metaDataFrame.SetValue(Grid.RowSpanProperty, 5);
            Grid.Children.Add(metaDataFrame);

            Frame widgetsFrame = new Frame();
            widgetsFrame.Content = widgetsDisplay.View;
            widgetsFrame.SetValue(Grid.RowProperty, 1);
            widgetsFrame.SetValue(Grid.ColumnProperty, 2);
            Grid.Children.Add(widgetsFrame);

            // start the renderer
            timer = new Timer(Render, null, 0, 40);

            // start the pipeline by connecting the reader to the pipeline
            // here this happens in the ChangeSmoothingFilter() method
            ChangeSegmentDetector();
            ChangeSmoothingFilter();
        }

        public void ViewUnloaded()
        {
            // TODO: dispose and clean up
            timer.Dispose();
            readerLinkColor?.Dispose();
            readerLinkPlot?.Dispose();
            readerLinkSkeleton?.Dispose();
            segmentationLink?.Dispose();
            smoothingLink?.Dispose();
            velocitySmoothingLink.Dispose();
            rowingSonification.Destroy();
        }

        public void ChangeSegmentDetector()
        {
            // Dispose links that link to the segment detector
            segmentationLink?.Dispose();

            if (UseZVC) {
                segmentDetector = new ZVCSegmentDetector(minimumHitGap,
                    startSegmentWithRisingVelocity);
                segmentationLink = velocitySmoothingFilter.Output.LinkTo(
                    segmentDetector.Input);
            }
            else {
                segmentDetector =
                    new DTWSegmentDetector(Properties.Settings.Default.DTWMaxDistance,
                    minimumSubsequenceLength);
                segmentationLink = shifter.Output.LinkTo(
                    segmentDetector.Input);
            }

            // rewire the pipeline
            segmentDetector.Output.LinkTo(jointDataPlot.PlotHitsBlock);
            segmentDetector.Output.LinkTo(kleshnevPlot.PlotHitsBlock);
            segmentDetector.Output.LinkTo(metaDataCalculator.InputSegmentHits);
            segmentDetector.Output.LinkTo(rowingSonification.InputSegmentHits);
        }

        public void ChangeSmoothingFilter()
        {
            // dispose links that link to the smoothing filter
            smoothingLink?.Dispose();
            velocitySmoothingLink?.Dispose();

            if (UseKinectJointFilter) {
                smoothingFilter = new KinectJointSmoothingFilter(
                    DataStreamType.SmoothedPosition);

                // Smoothed with some latency.
                // Filters out medium jitters.
                ((KinectJointSmoothingFilter)smoothingFilter).Init(
                    0.5f, 0.1f, 0.5f, 0.1f, 0.1f);

                // Very smooth, but with a lot of latency.
                // Filters out large jitters.
                velocitySmoothingFilter = new KinectJointSmoothingFilter(
                    DataStreamType.Velocity);
                ((KinectJointSmoothingFilter)velocitySmoothingFilter).Init(
                    0.7f, 0.3f, 1.0f, 1.0f, 1.0f);
            }
            else {
                smoothingFilter = new OneEuroSmoothingFilter(
                    DataStreamType.SmoothedPosition);
                ((OneEuroSmoothingFilter)smoothingFilter).Beta = Beta;
                ((OneEuroSmoothingFilter)smoothingFilter).Fcmin = Fcmin;

                velocitySmoothingFilter = new OneEuroSmoothingFilter(
                    DataStreamType.Velocity);
                ((OneEuroSmoothingFilter)velocitySmoothingFilter).Beta = Beta;
                ((OneEuroSmoothingFilter)velocitySmoothingFilter).Fcmin = Fcmin;
            }

            // link inputs of filters
            smoothingLink = kinectReader.JointDataBlock.LinkTo(smoothingFilter.Input);
            velocitySmoothingLink = velocityCalculator.Output.LinkTo(velocitySmoothingFilter.Input);

            // link ouptuts of filters
            smoothingFilter.Output.LinkTo(shifter.Input);
            smoothingFilter.Output.LinkTo(jointDataPlot.PlotJointDataBlock);
            velocitySmoothingFilter.Output.LinkTo(kleshnevVelocityCalculator.Input);
            velocitySmoothingFilter.Output.LinkTo(jointDataPlot.PlotJointDataBlock);

            if (UseZVC) {
                segmentationLink.Dispose();
                segmentationLink = velocitySmoothingFilter.Output.LinkTo(
                    segmentDetector.Input);
            }
        }

        public List<JointType> PlotJointTypes
        {
            get => jointDataPlot.PlotJointTypes;
            set => jointDataPlot.PlotJointTypes = value;
        }
        public List<DataStreamType> PlotMeasuredVariables
        {
            get => jointDataPlot.PlotMeasuredVariables;
            set => jointDataPlot.PlotMeasuredVariables = value;
        }
        public bool UseKinectJointFilter
        {
            get => useKinectJointFilter;
            set {
                useKinectJointFilter = value;
                ChangeSmoothingFilter();
            }
        }
        public bool UseZVC
        {
            get => useZVC;
            set {
                useZVC = value;
                ChangeSegmentDetector();
            }
        }
        public float PlotRange { get => plotRange; set => plotRange = value; }
        public Grid Grid { get => grid; set => grid = value; }
        public float KleshnevPlotRange { get => kleshnevPlotRange; set => kleshnevPlotRange = value; }
        public double Beta
        {
            get => beta;
            set {
                beta = value;
                ChangeSmoothingFilter();
            }
        }
        public double Fcmin
        {
            get => fcmin;
            set {
                fcmin = value;
                ChangeSmoothingFilter();
            }
        }
    }
}
