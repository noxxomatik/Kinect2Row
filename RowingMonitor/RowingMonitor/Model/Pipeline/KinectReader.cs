using Microsoft.Kinect;
using RowingMonitor.Model.EventArguments;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RowingMonitor.Model.Pipeline
{
    /// <summary>
    /// The KinectReader class connects the application to the Kinect device.
    /// 
    /// This class uses the singleton pattern with static initialization.
    /// 
    /// Get an instance of the class with:
    /// KinectReader kinectReader = KinectReader.Instance;
    /// </summary>
    public sealed class KinectReader
    {
        private static readonly KinectReader instance = new KinectReader();

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private MultiSourceFrameReader multiSourceFrameReader = null;

        /* frame descriptions */
        private FrameDescription depthFrameDescription;
        private FrameDescription colorFrameDescription;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        private long lastBroadcastedIndex = -1;

        /* Properties */
        public CoordinateMapper CoordinateMapper { get => coordinateMapper; }
        public string StatusText { get => statusText; }

        /* Events */
        public delegate void KinectFrameArrivedEventHandler(Object sender,
            KinectFrameArrivedEventArgs e);
        public event KinectFrameArrivedEventHandler KinectFrameArrived;

        public delegate void ColorFrameArrivedEventHandler(Object sender,
            EventArguments.ColorFrameArrivedEventArgs e);
        public event ColorFrameArrivedEventHandler ColorFrameArrived;

        /* Dataflow Block */
        private BroadcastBlock<JointData> jointDataBlock;
        private BroadcastBlock<WriteableBitmap> colorFrameBlock;

        // log times
        private List<double> timeLog = new List<double>();

        // check if reader already started
        private bool readerStarted = false;

        /// <summary>
        /// Initilizes the KinectReader class and establishes the connection to the sensor.
        /// </summary>
        private KinectReader()
        {
            // one sensor is currently supported
            kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            coordinateMapper = kinectSensor.CoordinateMapper;

            // get the extents
            DepthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;
            ColorFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;

            // open the reader for the body frames
            multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(
                FrameSourceTypes.Body | FrameSourceTypes.Color);

            // set IsAvailableChanged event notifier
            kinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            // open the sensor
            kinectSensor.Open();

            // set the status text
            statusText = kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                : Properties.Resources.NoSensorStatusText;

            // set the pieline blocks
            JointDataBlock = new BroadcastBlock<JointData>(jointData =>
            {
                return jointData;
            });

            ColorFrameBlock = new BroadcastBlock<WriteableBitmap>(bitmap =>
            {
                return bitmap;
            });
        }

        /// <summary>
        /// Instance of KinectReader singleton
        /// </summary>
        public static KinectReader Instance
        {
            get {
                return instance;
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable 
        /// (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            statusText = kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                : Properties.Resources.SensorNotAvailableStatusText;
        }

        private Tuple<WriteableBitmap, JointData> ReadMultiSourceFrame(MultiSourceFrame multiSourceFrame)
        {
            WriteableBitmap writableBitmap = null;
            JointData jointData = new JointData();

            using (BodyFrame bodyFrame =
                multiSourceFrame.BodyFrameReference.AcquireFrame()) {
                if (bodyFrame != null) {
                    if (bodies == null) {
                        bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, 
                    // Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed 
                    // and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    // update the kinect data container
                    JointDataHandler kdh = JointDataHandler.Instance;
                    kdh.Bodies = bodies;
                    if (kdh.GetFirstTrackedBody() != null) {
                        IReadOnlyDictionary<JointType, Joint> joints =
                            kdh.GetFirstTrackedBody().Joints;

                        // body frame available for other pipeline members
                        jointData = kdh.CreateNewJointData(
                                    bodyFrame.RelativeTime.TotalMilliseconds,
                                    DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond,
                                    joints);

                    }
                }
            }

            using (ColorFrame colorFrame =
                multiSourceFrame.ColorFrameReference.AcquireFrame()) {
                if (colorFrame != null) {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer()) {
                        WriteableBitmap bitmap = new WriteableBitmap(
                            colorFrameDescription.Width,
                            colorFrameDescription.Height,
                            96.0, 96.0, PixelFormats.Bgr32, null);
                        bitmap.Lock();
                        // verify data and write the new color frame data
                        // to the display bitmap
                        if ((colorFrameDescription.Width == bitmap.PixelWidth)
                            && (colorFrameDescription.Height == bitmap.PixelHeight)) {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                bitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width
                                * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            bitmap.AddDirtyRect(new Int32Rect(0, 0,
                                bitmap.PixelWidth,
                                bitmap.PixelHeight));
                        }
                        bitmap.Unlock();
                        writableBitmap = bitmap;
                        writableBitmap.Freeze();
                    }
                }
            }

            return new Tuple<WriteableBitmap, JointData>(writableBitmap, jointData);
        }

        /// <summary>
        /// Handles the multiple frame data arriving from sensor. 
        /// </summary>
        /// <param name="sender">Object sending the event.</param>
        /// <param name="e">Event arguments.</param>
        private void MultiSourceFrameReader_MultiSourceFrameArrived(object sender,
            MultiSourceFrameArrivedEventArgs e)
        {
            // process the multi source frame
            Tuple<WriteableBitmap, JointData> tuple = ReadMultiSourceFrame(e.FrameReference.AcquireFrame());

            // trigger the events
            // start the pipeline
            if (tuple.Item1 != null) {
                //ColorFrameArrived?(this, new ColorFrameArrivedEventArgs(tuple.Item1));
                ColorFrameBlock.Post(tuple.Item1);
            }
            if (tuple.Item2.Timestamps != null) {
                //KinectFrameArrived?(this, new KinectFrameArrivedEventArgs(tuple.Item2));
                JointDataBlock.Post(tuple.Item2);

                // log times
                timeLog.Add(tuple.Item2.AbsTimestamp);
                if (timeLog.Count == 100) {
                    Logger.LogTimestamps(timeLog, this.ToString(), "mean arrival of new frame");
                    timeLog = new List<double>();
                }
            }
        }

        /// <summary>
        /// Start the reader to aquire sensor data from the kinect sensor.
        /// </summary>
        public void StartReader()
        {
            if (this.multiSourceFrameReader != null && !readerStarted) {
                this.multiSourceFrameReader.MultiSourceFrameArrived +=
                    MultiSourceFrameReader_MultiSourceFrameArrived;
                readerStarted = true;
                Logger.Log(this.ToString(), "##### New Session #####");
            }           
        }

        /// <summary>
        /// Stop the kinect reader and clean up.
        /// </summary>
        public void StopReader()
        {
            if (this.multiSourceFrameReader != null) {
                // BodyFrameReader is IDisposable
                this.multiSourceFrameReader.Dispose();
                this.multiSourceFrameReader = null;
            }

            if (this.kinectSensor != null) {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }


        public BroadcastBlock<JointData> JointDataBlock
        {
            get => jointDataBlock;
            private set => jointDataBlock = value;
        }
        public BroadcastBlock<WriteableBitmap> ColorFrameBlock
        {
            get => colorFrameBlock;
            private set => colorFrameBlock = value;
        }
        public FrameDescription DepthFrameDescription
        {
            get => depthFrameDescription;
            private set => depthFrameDescription = value;
        }
        public FrameDescription ColorFrameDescription
        {
            get => colorFrameDescription;
            private set => colorFrameDescription = value;
        }
    }
}
