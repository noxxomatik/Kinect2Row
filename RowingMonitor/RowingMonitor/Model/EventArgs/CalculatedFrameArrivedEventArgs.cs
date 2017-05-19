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
    /// Represents the arguments for a calculated frame arrived event.
    /// </summary>
    public class CalculatedFrameArrivedEventArgs : EventArgs
    {
        private JointData calculatedJointData;

        public CalculatedFrameArrivedEventArgs(JointData calculatedJointData)
        {
            CalculatedJointData = calculatedJointData;
        }

        public JointData CalculatedJointData
        {
            get => calculatedJointData;
            private set => calculatedJointData = value;
        }
    }
}
