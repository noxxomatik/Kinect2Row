using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Pipeline
{
    public abstract class SmoothingFilter
    {
        public delegate void SmoothedFrameArrivedEventHandler(Object sender,
            SmoothedFrameArrivedEventArgs e);
        public event SmoothedFrameArrivedEventHandler SmoothedFrameArrived;

        public abstract void Update(JointData jointData);

        protected virtual void OnSmoothedFrameFinished(SmoothedFrameArrivedEventArgs e)
        {
            SmoothedFrameArrived?.Invoke(this, e);
        }
    }
}
