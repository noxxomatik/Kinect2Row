using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    public enum KleshnevVelocityType
    {
        Legs,
        HandleRight,
        HandleLeft,
        Trunk,
        ArmsRight,
        ArmsLeft
    }

    public enum PlotOptionsMeasuredVariables
    {
        RawPosition,
        SmoothedPosition,
        Velocity,
        SegmentHits
    }
}
