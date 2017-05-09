using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model
{
    /// <summary>
    /// The KinectReader class connects the application to the Kinect device.
    /// 
    /// This class uses the singleton pattern with static initialization.
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
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /* Properties */
        public CoordinateMapper CoordinateMapper { get => coordinateMapper; }
        public BodyFrameReader BodyFrameReader { get => bodyFrameReader; }
        public int DisplayWidth { get => displayWidth; }
        public int DisplayHeight { get => displayHeight; }
        public string StatusText { get => statusText; }

        /* Events */
        public delegate void KinectFrameArrivedEventHandler(Object sender, KinectFrameArrivedEventArgs e);
        public event KinectFrameArrivedEventHandler KinectFrameArrived;

        /// <summary>
        /// Initilizes the KinectReader class and establishes the connection to the sensor.
        /// </summary>
        private KinectReader()
        {
            // one sensor is currently supported
            kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            coordinateMapper = kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            displayWidth = frameDescription.Width;
            displayHeight = frameDescription.Height;

            // open the reader for the body frames
            bodyFrameReader = kinectSensor.BodyFrameSource.OpenReader();

            // set IsAvailableChanged event notifier
            kinectSensor.IsAvailableChanged += Sensor_IsAvailableChanged;

            // open the sensor
            kinectSensor.Open();

            // set the status text
            statusText = kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText 
                : Properties.Resources.NoSensorStatusText;
        }

        /// <summary>
        /// Instance of KinectReader singleton
        /// </summary>
        public static KinectReader Instance
        {
            get 
            {
                return instance;
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            statusText = kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                : Properties.Resources.SensorNotAvailableStatusText;
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame()) {                
                if (bodyFrame != null) {
                    if (bodies == null) {
                        bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    // update the kinect data container
                    KinectDataContainer kdc = KinectDataContainer.Instance;
                    kdc.Bodies = bodies;
                    if (kdc.GetFirstTrackedBody() != null) {
                        IReadOnlyDictionary<JointType, Joint> joints = kdc.GetFirstTrackedBody().Joints;
                        kdc.AddNewRawJointData(bodyFrame.RelativeTime.TotalMilliseconds, joints);
                        // body frame available for other pipeline members
                        KinectFrameArrived(this, new KinectFrameArrivedEventArgs());
                    }
                }
            }
        }

        public void StartReader()
        {
            if (this.bodyFrameReader != null) {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        public void StopReader()
        {
            if (this.bodyFrameReader != null) {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null) {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }
    }
}
