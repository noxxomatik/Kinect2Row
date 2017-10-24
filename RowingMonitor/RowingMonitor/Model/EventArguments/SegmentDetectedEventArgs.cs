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
    /// Represents the arguments for a detected segment event.
    /// </summary>
    public class SegmentDetectedEventArgs : EventArgs
    {
        private List<SegmentHit> hits;

        public SegmentDetectedEventArgs(List<SegmentHit> hits)
        {
            Hits = hits;
        }

        public List<SegmentHit> Hits { get => hits; private set => hits = value; }
    }
}
