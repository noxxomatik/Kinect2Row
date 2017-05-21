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
        private List<long> hitIndices;
        private List<double> hitTimestamps;

        public SegmentDetectedEventArgs(List<long> hitIndices, List<double> hitTimestamps)
        {
            HitIndices = hitIndices;
            HitTimestamps = hitTimestamps;
        }
        public List<double> HitTimestamps { get => hitTimestamps; private set => hitTimestamps = value; }
        public List<long> HitIndices { get => hitIndices; private set => hitIndices = value; }
    }
}
