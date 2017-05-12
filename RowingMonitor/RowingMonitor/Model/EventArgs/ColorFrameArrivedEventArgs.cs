using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model
{
    /// <summary>
    /// Represents the arguments for a KinectReader's ColorFrameArrived event.
    /// </summary>
    public class ColorFrameArrivedEventArgs : EventArgs        
    {
        private KinectDataContainer kinectDataContainer = KinectDataContainer.Instance;
        public KinectDataContainer KinectDataContainer { get => kinectDataContainer; }

        public ColorFrameArrivedEventArgs(){}        
    }
}
