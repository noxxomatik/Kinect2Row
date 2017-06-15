using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    public abstract class SmoothingFilter
    {
        public delegate void SmoothedFrameArrivedEventHandler(Object sender,
            SmoothedFrameArrivedEventArgs e);
        public event SmoothedFrameArrivedEventHandler SmoothedFrameArrived;

        private TransformBlock<JointData, JointData> smoothingBlock;

        public TransformBlock<JointData, JointData> SmoothingBlock { get => smoothingBlock; set => smoothingBlock = value; }

        public abstract JointData Smooth(JointData jointData);

        public abstract void Update(JointData jointData);

        protected virtual void OnSmoothedFrameFinished(SmoothedFrameArrivedEventArgs e)
        {
            SmoothedFrameArrived?.Invoke(this, e);
        }
    }
}
