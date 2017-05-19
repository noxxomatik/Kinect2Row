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
        private List<long> hitList;

        public SegmentDetectedEventArgs(List<long> hitList)
        {
            HitList = hitList;
        }

        public List<long> HitList { get => hitList; private set => hitList = value; }
    }
}
