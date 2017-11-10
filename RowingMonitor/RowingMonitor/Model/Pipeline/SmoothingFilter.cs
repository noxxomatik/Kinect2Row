using RowingMonitor.Model.EventArguments;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    /// <summary>
    /// Abstract class of a smoothing filter to generalize the usage of different methods for value smoothing.
    /// </summary>
    public abstract class SmoothingFilter
    {
        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void SmoothedFrameArrivedEventHandler(Object sender,
            SmoothedFrameArrivedEventArgs e);
        /// <summary>
        /// DEPRECATED
        /// </summary>
        public event SmoothedFrameArrivedEventHandler SmoothedFrameArrived;

        private ActionBlock<JointData> input;
        private BroadcastBlock<JointData> output;

        private DataStreamType outputDataStreamType;        

        /// <summary>
        /// Uses the smoothing method of the derived class to filter the input values.
        /// </summary>
        /// <param name="jointData">The JointData to be smoothed.</param>
        /// <returns>Returns the smoothed JointData.</returns>
        public abstract JointData Smooth(JointData jointData);

        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="jointData"></param>
        public abstract void Update(JointData jointData);

        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSmoothedFrameFinished(SmoothedFrameArrivedEventArgs e)
        {
            SmoothedFrameArrived?.Invoke(this, e);
        }

        /// <summary>
        /// BroadcastBlock for sending the smoothed JointData to all linked traget blocks.
        /// </summary>
        public BroadcastBlock<JointData> Output { get => output; set => output = value; }
        /// <summary>
        /// The DataStreamType of the resulting values. Dependant of the input DataStreamType.
        /// </summary>
        public DataStreamType OutputDataStreamType { get => outputDataStreamType; set => outputDataStreamType = value; }
        /// <summary>
        /// ActionBlock that recieves JointData.
        /// </summary>
        public ActionBlock<JointData> Input { get => input; set => input = value; }
    }
}
