using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model
{
    /// <summary>
    /// Represents the arguments for a detected segment event.
    /// </summary>
    public class SegmentDetectedEventArgs : EventArgs        
    {
        private KinectDataContainer kinectDataContainer = KinectDataContainer.Instance;
        public KinectDataContainer KinectDataContainer { get => kinectDataContainer; }

        public SegmentDetectedEventArgs(){}        
    }
}
