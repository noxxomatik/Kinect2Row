using RowingMonitor.Model.Pipeline;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Controls;

namespace RowingMonitor.ViewModel
{
    class CoachViewModel : INotifyPropertyChanged
    {
        /* pipeline elements */
        // kinect reader
        private KinectReader kinectReader;
        // smoothing filter
        private SmoothingFilter smoothingFilter;
        // velocity smoothing filter
        private SmoothingFilter velocitySmoothingFilter;
        // origin shifter
        private Shifter shifter;
        // segmentation
        private SegmentDetector segmentDetector;
        // calculations
        private VelocityCalculator velocityCalculator;
        private KleshnevVelocityCalculator kleshnevVelocityCalculator;
        // meta data
        private RowingMetadataCalculator metaDataCalculator;
        // outputs
        private SkeletonSideDisplay skeletonSideDisplay;
        private TrunkAngleDisplay trunkAngleDisplay;
        private RowingSonification rowingSonification;
        private RowingMetadataWidgetsDisplay widgetsDisplay;
        private KleshnevPlot kleshnevPlot;
        // view specific
        private ActionBlock<RowingMetadata> viewInput;

        /* pipeline links */
        private IDisposable readerLink;

        /* options */
        // ZVC
        private int minimumHitGap = 10;
        private bool startSegmentWithRisingVelocity = true;
        // kleshnev plot
        private float kleshnevPlotRange = 3;

        /* gui */
        private Grid mainGrid;
        // render timer
        private Timer timer;
        // meta data
        private string sessionTime;
        private string strokeCount;
        private string strokeRate;
        private string strokeLength;
        private string meanStrokeLength;
        private string seatTravel;
        private string meanSeatTravel;
        private string strokeTime;
        private string meanStrokeTime;
        private string maxHandleVelocity;
        private string meanMaxHandleVelocity;
        private string maxLegsVelocity;
        private string meanMaxLegsVelocity;
        private string maxArmsVelocity;
        private string meanMaxArmsVelocity;
        private string maxTrunkVelocity;
        private string meanMaxTrunkVelocity;
        private string meanCatchFactor;
        private string meanRowingStyleFactor;

        public CoachViewModel()
        {
            /* create pipeline */
            kinectReader = KinectReader.Instance;
            smoothingFilter = new OneEuroSmoothingFilter(DataStreamType.SmoothedPosition);
            shifter = new Shifter();
            velocityCalculator = new VelocityCalculator();
            velocitySmoothingFilter = new OneEuroSmoothingFilter(DataStreamType.Velocity);
            segmentDetector = new ZVCSegmentDetector(minimumHitGap, startSegmentWithRisingVelocity);
            kleshnevVelocityCalculator = new KleshnevVelocityCalculator();
            metaDataCalculator = new RowingMetadataCalculator();

            skeletonSideDisplay = new SkeletonSideDisplay();
            trunkAngleDisplay = new TrunkAngleDisplay();
            rowingSonification = new RowingSonification();
            widgetsDisplay = new RowingMetadataWidgetsDisplay();
            kleshnevPlot = new KleshnevPlot(kleshnevPlotRange);


            viewInput = new ActionBlock<RowingMetadata>(metaData =>
            {
                UpdateRowingMetaData(metaData);
            });

            /* link the pipeline */
            //readerLink = kinectReader.JointDataBlock.LinkTo(smoothingFilter.Input);

            smoothingFilter.Output.LinkTo(shifter.Input);

            shifter.Output.LinkTo(velocityCalculator.Input);
            shifter.Output.LinkTo(metaDataCalculator.Input);
            shifter.Output.LinkTo(trunkAngleDisplay.Input);
            shifter.Output.LinkTo(skeletonSideDisplay.SkeletonBlock);

            velocityCalculator.Output.LinkTo(velocitySmoothingFilter.Input);

            velocitySmoothingFilter.Output.LinkTo(kleshnevVelocityCalculator.Input);
            velocitySmoothingFilter.Output.LinkTo(segmentDetector.Input);

            kleshnevVelocityCalculator.Output.LinkTo(metaDataCalculator.InputKleshnevData);
            kleshnevVelocityCalculator.Output.LinkTo(rowingSonification.Input);
            kleshnevVelocityCalculator.Output.LinkTo(kleshnevPlot.KleshnevDataBlock);

            segmentDetector.Output.LinkTo(metaDataCalculator.InputSegmentHits);
            segmentDetector.Output.LinkTo(rowingSonification.InputSegmentHits);
            segmentDetector.Output.LinkTo(kleshnevPlot.PlotHitsBlock);

            metaDataCalculator.Output.LinkTo(widgetsDisplay.Input);
            metaDataCalculator.Output.LinkTo(viewInput);
        }

        private void Render(object state)
        {
            /* render view elements */
            skeletonSideDisplay.Render();
            trunkAngleDisplay.Render();
            widgetsDisplay.Render();
            kleshnevPlot.Render();
        }

        public void ViewLoaded()
        {
            /* link the gui */
            AddGridElement(kleshnevPlot.View, 0, 1, 1, 3);
            AddGridElement(widgetsDisplay.View, 1, 1, 1, 1);
            AddGridElement(skeletonSideDisplay.View, 1, 2, 2, 2);
            AddGridElement(trunkAngleDisplay.View, 2, 1, 2, 1);

            /* start the renderer */
            timer = new Timer(Render, null, 0, 40);

            /* start the pipeline by connecting the reader to the rest of the pipeline */
            readerLink = kinectReader.JointDataBlock.LinkTo(smoothingFilter.Input);
        }

        public void ViewUnloaded()
        {
            /* dispose all elements*/
            timer.Dispose();
            readerLink.Dispose();
            rowingSonification.Destroy();
        }

        private void AddGridElement(object content, int row,
            int column, int rowSpan = 1, int colomnSpan = 1)
        {
            Frame frame = new Frame();
            frame.Content = content;
            frame.SetValue(Grid.RowProperty, row);
            frame.SetValue(Grid.RowSpanProperty, rowSpan);
            frame.SetValue(Grid.ColumnProperty, column);
            frame.SetValue(Grid.ColumnSpanProperty, colomnSpan);
            MainGrid.Children.Add(frame);
        }

        private void UpdateRowingMetaData(RowingMetadata metaData)
        {
            // prepare the meta data for the view
            TimeSpan time = TimeSpan.FromMilliseconds(metaData.SessionTime);
            SessionTime = time.ToString(@"hh\:mm\:ss");

            if (metaData.StrokeCount != 0) {
                StrokeCount = metaData.StrokeCount.ToString();
            }

            if (metaData.StrokesPerMinute != 0) {
                StrokeRate = metaData.StrokesPerMinute.ToString("0.0");
            }

            if (metaData.StrokeLength != 0) {
                StrokeLength = metaData.StrokeLength.ToString("0.00");
            }
            if (metaData.MeanStrokeLength != 0) {
                MeanStrokeLength = metaData.MeanStrokeLength.ToString("0.00");
            }

            if (metaData.StrokeTime != 0) {
                StrokeTime = (metaData.StrokeTime / 1000).ToString("0.00");
            }
            if (metaData.MeanStrokeTime != 0) {
                MeanStrokeTime = (metaData.MeanStrokeTime / 1000).ToString("0.00");
            }

            if (metaData.SeatTravelDistance != 0) {
                SeatTravel = metaData.SeatTravelDistance.ToString("0.00");
            }
            if (metaData.MeanSeatTravelDistance != 0) {
                MeanSeatTravel = metaData.MeanSeatTravelDistance.ToString("0.00");
            }

            if (metaData.MaxHandleVelocity != 0) {
                MaxHandleVelocity = metaData.MaxHandleVelocity.ToString("0.0");
            }
            if (metaData.MeanMaxHandleVelocity != 0) {
                MeanMaxHandleVelocity = metaData.MeanMaxHandleVelocity.ToString("0.0");
            }

            if (metaData.MaxLegsVelocity != 0) {
                MaxLegsVelocity = metaData.MaxLegsVelocity.ToString("0.0");
            }
            if (metaData.MeanMaxLegsVelocity != 0) {
                MeanMaxLegsVelocity = metaData.MeanMaxLegsVelocity.ToString("0.0");
            }

            if (metaData.MaxArmsVelocity != 0) {
                MaxArmsVelocity = metaData.MaxArmsVelocity.ToString("0.0");
            }
            if (metaData.MeanMaxArmsVelocity != 0) {
                MeanMaxArmsVelocity = metaData.MeanMaxArmsVelocity.ToString("0.0");
            }

            if (metaData.MaxTrunkVelocity != 0) {
                MaxTrunkVelocity = metaData.MaxTrunkVelocity.ToString("0.0");
            }
            if (metaData.MeanMaxTrunkVelocity != 0) {
                MeanMaxTrunkVelocity = metaData.MeanMaxTrunkVelocity.ToString("0.0");
            }

            if(metaData.MeanCatchFactor != 0) {
                MeanCatchFactor = metaData.MeanCatchFactor.ToString("0.0");
            }

            if (metaData.MeanRowingStyleFactor != 0) {
                MeanRowingStyleFactor = (metaData.MeanRowingStyleFactor * 100).ToString("0.0");
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "None")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /* properties */
        public event PropertyChangedEventHandler PropertyChanged;


        public Grid MainGrid { get => mainGrid; set => mainGrid = value; }
        public string SessionTime { get => sessionTime; set { sessionTime = value; OnPropertyChanged(); } }
        public string StrokeCount { get => strokeCount; set { strokeCount = value; OnPropertyChanged(); } }
        public string StrokeRate { get => strokeRate; set { strokeRate = value; OnPropertyChanged(); } }
        public string StrokeLength { get => strokeLength; set { strokeLength = value; OnPropertyChanged(); } }
        public string MeanStrokeLength { get => meanStrokeLength; set { meanStrokeLength = value; OnPropertyChanged(); } }
        public string SeatTravel { get => seatTravel; set { seatTravel = value; OnPropertyChanged(); } }
        public string MeanSeatTravel { get => meanSeatTravel; set { meanSeatTravel = value; OnPropertyChanged(); } }
        public string StrokeTime { get => strokeTime; set { strokeTime = value; OnPropertyChanged(); } }
        public string MeanStrokeTime { get => meanStrokeTime; set { meanStrokeTime = value; OnPropertyChanged(); } }
        public string MaxHandleVelocity { get => maxHandleVelocity; set { maxHandleVelocity = value; OnPropertyChanged(); } }
        public string MeanMaxHandleVelocity { get => meanMaxHandleVelocity; set { meanMaxHandleVelocity = value; OnPropertyChanged(); } }
        public string MaxLegsVelocity { get => maxLegsVelocity; set { maxLegsVelocity = value; OnPropertyChanged(); } }
        public string MeanMaxLegsVelocity { get => meanMaxLegsVelocity; set { meanMaxLegsVelocity = value; OnPropertyChanged(); } }
        public string MaxArmsVelocity { get => maxArmsVelocity; set { maxArmsVelocity = value; OnPropertyChanged(); } }
        public string MeanMaxArmsVelocity { get => meanMaxArmsVelocity; set { meanMaxArmsVelocity = value; OnPropertyChanged(); } }
        public string MaxTrunkVelocity { get => maxTrunkVelocity; set { maxTrunkVelocity = value; OnPropertyChanged(); } }
        public string MeanMaxTrunkVelocity { get => meanMaxTrunkVelocity; set { meanMaxTrunkVelocity = value; OnPropertyChanged(); } }
        public string MeanCatchFactor { get => meanCatchFactor; set { meanCatchFactor = value; OnPropertyChanged(); } }
        public string MeanRowingStyleFactor { get => meanRowingStyleFactor; set { meanRowingStyleFactor = value; OnPropertyChanged(); } }
    }
}
