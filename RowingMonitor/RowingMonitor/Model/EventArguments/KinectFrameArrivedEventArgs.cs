using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.EventArguments
{
    /// <summary>
    /// Represents the arguments for a KinectReader's FrameArrived event.
    /// </summary>
    public class KinectFrameArrivedEventArgs : EventArgs
    {
        private JointData jointData;

        public KinectFrameArrivedEventArgs(JointData jointData)
        {
            JointData = jointData;
        }

        public JointData JointData { get => jointData; private set => jointData = value; }
    }
}
