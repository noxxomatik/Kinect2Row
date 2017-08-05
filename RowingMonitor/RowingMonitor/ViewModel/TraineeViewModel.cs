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
    class TraineeViewModel : INotifyPropertyChanged
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
        private RowingMetaDataCalculator metaDataCalculator;
        // outputs
        private SkeletonSideDisplay skeletonSideDisplay;
        private TrunkAngleDisplay trunkAngleDisplay;
        private RowingSonification rowingSonification;
        private RowingMetaDataWidgetsDisplay widgetsDisplay;
        // view specific
        private ActionBlock<RowingMetaData> viewInput;

        /* pipeline links */
        private IDisposable readerLink;

        /* options */
        // ZVC
        int minimumHitGap = 10;
        bool startSegmentWithRisingVelocity = true;

        /* gui */
        private Grid mainGrid;
        // render timer
        private Timer timer;
        // meta data
        private string sessionTime;
        private string strokeCount;
        private string strokeRate;
        private string meanStrokeLength;
        private string meanSeatTravel;
        private string meanStrokeTime;

        public TraineeViewModel()
        {
            /* create pipeline */
            kinectReader = KinectReader.Instance;
            smoothingFilter = new OneEuroSmoothingFilter(DataStreamType.SmoothedPosition);
            shifter = new Shifter();
            velocityCalculator = new VelocityCalculator();
            velocitySmoothingFilter = new OneEuroSmoothingFilter(DataStreamType.Velocity);
            segmentDetector = new ZVCSegmentDetector(minimumHitGap, startSegmentWithRisingVelocity);
            kleshnevVelocityCalculator = new KleshnevVelocityCalculator();
            metaDataCalculator = new RowingMetaDataCalculator();

            skeletonSideDisplay = new SkeletonSideDisplay();
            trunkAngleDisplay = new TrunkAngleDisplay();
            rowingSonification = new RowingSonification();
            widgetsDisplay = new RowingMetaDataWidgetsDisplay();

            viewInput = new ActionBlock<RowingMetaData>(metaData =>
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

            segmentDetector.Output.LinkTo(metaDataCalculator.InputSegmentHits);

            metaDataCalculator.Output.LinkTo(widgetsDisplay.Input);
            metaDataCalculator.Output.LinkTo(viewInput);
        }

        private void Render(object state)
        {
            /* render view elements */
            skeletonSideDisplay.Render();
            trunkAngleDisplay.Render();
            widgetsDisplay.Render();
        }

        public void ViewLoaded()
        {
            /* link the gui */
            AddGridElement(skeletonSideDisplay.View, 1, 2, 3, 5);
            AddGridElement(widgetsDisplay.View, 1, 0, 2, 2);
            AddGridElement(trunkAngleDisplay.View, 3, 0, 2, 2);

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

        private void UpdateRowingMetaData(RowingMetaData metaData)
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

            if (metaData.MeanStrokeLength != 0) {
                MeanStrokeLength = metaData.MeanStrokeLength.ToString("0.00") + "m";
            }

            if (metaData.MeanSeatTravelDistance != 0) {
                MeanSeatTravel = metaData.MeanSeatTravelDistance.ToString("0.00") + "m";
            }

            if(metaData.MeanStrokeTime != 0) {
                MeanStrokeTime = metaData.MeanStrokeTime.ToString("0.00") + "s";
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "None")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /* properties */
        public event PropertyChangedEventHandler PropertyChanged;
        public string SessionTime
        {
            get => sessionTime;
            set {
                sessionTime = value;
                OnPropertyChanged();
            }
        }
        public string StrokeCount
        {
            get => strokeCount;
            set {
                strokeCount = value;
                OnPropertyChanged();
            }
        }
        public string StrokeRate
        {
            get => strokeRate;
            set {
                strokeRate = value;
                OnPropertyChanged();
            }
        }
        public string MeanStrokeLength
        {
            get => meanStrokeLength;
            set {
                meanStrokeLength = value;
                OnPropertyChanged();
            }
        }
        public string MeanSeatTravel
        {
            get => meanSeatTravel;
            set {
                meanSeatTravel = value;
                OnPropertyChanged();
            }
        }
        public string MeanStrokeTime
        {
            get => meanStrokeTime;
            set {
                meanStrokeTime = value;
                OnPropertyChanged();
            }
        }

        public Grid MainGrid { get => mainGrid; set => mainGrid = value; }
    }
}
