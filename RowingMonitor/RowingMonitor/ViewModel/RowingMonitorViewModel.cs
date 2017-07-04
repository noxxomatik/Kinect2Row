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
        private SkeletonFrontalDisplay skeletonFrontalDisplay;
        private SkeletonSideDisplay skeletonSideDisplay;
        private KleshnevPlot kleshnevPlot;

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

        // render timer
        private Timer timer;

        public RowingMonitorViewModel()
        {
            // create the pipeline elements
            kinectReader = KinectReader.Instance;
            //smoothingFilter = new OneEuroSmoothingFilter(DataStreamType.SmoothedPosition);
            shifter = new Shifter();
            velocityCalculator = new VelocityCalculator();
            //velocitySmoothingFilter = new OneEuroSmoothingFilter(DataStreamType.Velocity);
            segmentDetector = new ZVCSegmentDetector(minimumHitGap, startSegmentWithRisingVelocity);
            kleshnevVelocityCalculator = new KleshnevVelocityCalculator();

            jointDataPlot = new JointDataPlot(PlotRange);
            skeletonFrontalDisplay = new SkeletonFrontalDisplay(kinectReader.CoordinateMapper,
                kinectReader.DisplayWidth, kinectReader.DisplayHeight);
            skeletonSideDisplay = new SkeletonSideDisplay();
            kleshnevPlot = new KleshnevPlot(KleshnevPlotRange);

            // link pipeline together
            //kinectReader.JointDataBlock.LinkTo(smoothingFilter.SmoothingBlock);
            kinectReader.JointDataBlock.LinkTo(jointDataPlot.PlotJointDataBlock);
            kinectReader.JointDataBlock.LinkTo(skeletonFrontalDisplay.SkeletonBlock);
            kinectReader.ColorFrameBlock.LinkTo(skeletonFrontalDisplay.ColorImageBlock);            

            //smoothingFilter.SmoothingBlock.LinkTo(shifter.ShiftingBlock);
            //smoothingFilter.SmoothingBlock.LinkTo(jointDataPlot.PlotJointDataBlock);

            shifter.Output.LinkTo(velocityCalculator.Input);
            shifter.Output.LinkTo(jointDataPlot.PlotJointDataBlock);
            shifter.Output.LinkTo(skeletonSideDisplay.SkeletonBlock);

            //velocityCalculator.CalculationBlock.LinkTo(
            //    velocitySmoothingFilter.SmoothingBlock);

            //velocitySmoothingFilter.SmoothingBlock.LinkTo(kleshnevVelocityCalculator.CalculationBlock);
            ////velocitySmoothingFilter.SmoothingBlock.LinkTo(segmentDetector.DetectionInputBlock);
            //velocitySmoothingFilter.SmoothingBlock.LinkTo(jointDataPlot.PlotJointDataBlock);

            kleshnevVelocityCalculator.Output.LinkTo(kleshnevPlot.KleshnevDataBlock);

            //segmentDetector.DetectionOutputBlock.LinkTo(jointDataPlot.PlotHitsBlock);
            //segmentDetector.DetectionOutputBlock.LinkTo(kleshnevPlot.PlotHitsBlock);

            ChangeSegmentDetector();
            ChangeSmoothingFilter();

            timer = new Timer(Render, null, 0, 40);
        }

        private void Render(object state)
        {
            jointDataPlot.Render();
            kleshnevPlot.Render();
            skeletonSideDisplay.Render();
            skeletonFrontalDisplay.Render();
        }

        public void WindowLoaded()
        {
            // create the GUI    
            Frame plotFrame = new Frame();
            plotFrame.Content = jointDataPlot.View;
            plotFrame.SetValue(Grid.RowProperty, 1);
            plotFrame.SetValue(Grid.ColumnProperty, 0);
            Grid.Children.Add(plotFrame);

            Frame frontalDisplayFrame = new Frame();
            frontalDisplayFrame.Content = skeletonFrontalDisplay.View;
            frontalDisplayFrame.SetValue(Grid.RowProperty, 0);
            frontalDisplayFrame.SetValue(Grid.ColumnProperty, 0);
            Grid.Children.Add(frontalDisplayFrame);

            Frame sideDisplayFrame = new Frame();
            sideDisplayFrame.Content = skeletonSideDisplay.View;
            sideDisplayFrame.SetValue(Grid.RowProperty, 0);
            sideDisplayFrame.SetValue(Grid.ColumnProperty, 1);
            Grid.Children.Add(sideDisplayFrame);

            Frame kleshnevPlotFrame = new Frame();
            kleshnevPlotFrame.Content = kleshnevPlot.View;
            kleshnevPlotFrame.SetValue(Grid.RowProperty, 2);
            kleshnevPlotFrame.SetValue(Grid.ColumnProperty, 0);
            kleshnevPlotFrame.SetValue(Grid.ColumnSpanProperty, 2);
            Grid.Children.Add(kleshnevPlotFrame);

            // start the pipeline
            kinectReader.StartReader();
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
                segmentDetector = new DTWSegmentDetector(distanceThreshold,
                    minimumSubsequenceLength);
                segmentationLink = shifter.Output.LinkTo(
                    segmentDetector.Input);
            }

            // rewire the pipeline
            segmentDetector.Output.LinkTo(jointDataPlot.PlotHitsBlock);
            segmentDetector.Output.LinkTo(kleshnevPlot.PlotHitsBlock);
        }

        public void ChangeSmoothingFilter()
        {
            // Dispose links that link to the segment detector
            smoothingLink?.Dispose();
            velocitySmoothingLink?.Dispose();

            if (UseKinectJointFilter) {
                smoothingFilter = new KinectJointSmoothingFilter(
                    DataStreamType.SmoothedPosition);
                //kinectJointFilter.Init();   // suggested value
                // Some smoothing with little latency (defaults).
                // Only filters out small jitters.
                // Good for gesture recognition in games.
                //kinectJointFilter.Init(0.5f, 0.5f, 0.5f, 0.05f, 0.04f);
                // Smoothed with some latency.
                // Filters out medium jitters.
                // Good for a menu system that needs to be smooth but
                // doesn't need the reduced latency as much as gesture recognition does.
                ((KinectJointSmoothingFilter)smoothingFilter).Init(
                    0.5f, 0.1f, 0.5f, 0.1f, 0.1f);
                // Very smooth, but with a lot of latency.
                // Filters out large jitters.
                // Good for situations where smooth data is absolutely required
                // and latency is not an issue.
                //kinectJointFilter.Init(0.7f, 0.3f, 1.0f, 1.0f, 1.0f);                

                velocitySmoothingFilter = new KinectJointSmoothingFilter(
                    DataStreamType.Velocity);
                ((KinectJointSmoothingFilter)velocitySmoothingFilter).Init(
                    0.7f, 0.3f, 1.0f, 1.0f, 1.0f);

            }
            else {
                smoothingFilter = new OneEuroSmoothingFilter(
                    DataStreamType.SmoothedPosition);
                ((OneEuroSmoothingFilter)smoothingFilter).Beta = 0.0;
                ((OneEuroSmoothingFilter)smoothingFilter).Fcmin = 1.0;

                velocitySmoothingFilter = new OneEuroSmoothingFilter(
                    DataStreamType.Velocity);
                ((OneEuroSmoothingFilter)velocitySmoothingFilter).Beta = 0.0;
                ((OneEuroSmoothingFilter)velocitySmoothingFilter).Fcmin = 1.0;
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

        public List<JointType> PlotJointTypes {
            get => jointDataPlot.PlotJointTypes; set => jointDataPlot.PlotJointTypes = value; }
        public List<DataStreamType> PlotMeasuredVariables {
            get => jointDataPlot.PlotMeasuredVariables; set => jointDataPlot.PlotMeasuredVariables = value; }
        public bool UseKinectJointFilter { get => useKinectJointFilter; set => useKinectJointFilter = value; }
        public bool UseZVC { get => useZVC; set => useZVC = value; }
        public float PlotRange { get => plotRange; set => plotRange = value; }
        public Grid Grid { get => grid; set => grid = value; }
        public float KleshnevPlotRange { get => kleshnevPlotRange; set => kleshnevPlotRange = value; }
    }
}