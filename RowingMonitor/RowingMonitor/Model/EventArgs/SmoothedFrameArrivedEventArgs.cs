using Microsoft.Kinect;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model
{
    /// <summary>
    /// Represents the arguments for a smoothed joint data arrived event.
    /// </summary>
    public class SmoothedFrameArrivedEventArgs : EventArgs
    {
        private JointData rawJointData;
        private JointData smoothedJointData;

        public SmoothedFrameArrivedEventArgs(JointData rawJointData,
            JointData smoothedJointData)
        {
            RawJointData = rawJointData;
            SmoothedJointData = smoothedJointData;
        }

        public JointData RawJointData
        {
            get => rawJointData;
            private set => rawJointData = value;
        }
        public JointData SmoothedJointData
        {
            get => smoothedJointData;
            private set => smoothedJointData = value;
        }
    }
}
