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
    /// Represents the arguments for a shifted frame arrived event.
    /// </summary>
    public class ShiftedFrameArrivedEventArgs : EventArgs
    {
        private JointData shiftedJointData;

        public ShiftedFrameArrivedEventArgs(JointData shiftedJointData)
        {
            ShiftedJointData = shiftedJointData;
        }

        public JointData ShiftedJointData
        {
            get => shiftedJointData;
            private set => shiftedJointData = value;
        }
    }
}
