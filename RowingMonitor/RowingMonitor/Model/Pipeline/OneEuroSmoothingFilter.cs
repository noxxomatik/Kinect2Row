﻿using Microsoft.Kinect;
using RowingMonitor.Model.EventArguments;
using RowingMonitor.Model.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace RowingMonitor.Model.Pipeline
{
    /// <summary>
    /// Implements the 1€ Filter from Casiez et al.
    /// </summary>
    public class OneEuroSmoothingFilter : SmoothingFilter
    {
        // data update rate in Hz (default is 30 FPS)
        private double rate = 120;
        // minimum cutoff frequency
        private double fcmin;
        private Dictionary<JointType, Dictionary<String, Double>> mincutoff =
            new Dictionary<JointType, Dictionary<string, double>>();
        // cutoff slope
        private double beta;
        // cutoff frequency for derivate
        private Dictionary<JointType, Dictionary<String, Double>> dcutoff =
            new Dictionary<JointType, Dictionary<string, double>>();
        // low pass filter
        LowPassFilter xfilt;
        // low-pass filter for derivate
        LowPassFilter dxfilt;

        JointData lastJointData;

        private bool firstTime = true;          

        private List<double> timeLog = new List<double>();

        /// <summary>
        /// Creates a new instance of the 1€ Filter.
        /// </summary>
        /// <param name="outputDataStreamType">The DataStreamType of the output JointData.</param>
        public OneEuroSmoothingFilter(DataStreamType outputDataStreamType)
        {
            OutputDataStreamType = outputDataStreamType;

            Beta = 0.0;
            Fcmin = 1.0;
            Mincutoff = InitCutoffDictionary(Fcmin);

            dcutoff = InitCutoffDictionary(1.0);

            xfilt = new LowPassFilter();
            dxfilt = new LowPassFilter();

            Input = new ActionBlock<JointData>(jointData =>
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                if (JointDataHandler.IsValid(jointData)) {
                    Output.Post(Smooth(jointData));
                }

                stopwatch.Stop();
                // log times
                timeLog.Add(stopwatch.Elapsed.TotalMilliseconds);
                if (timeLog.Count == 100) {
                    Logger.LogTimes(timeLog, this.ToString(),
                        "mean time to smooth for " + OutputDataStreamType);
                    timeLog = new List<double>();
                }
            });

            Output = new BroadcastBlock<JointData>(jointData =>
            {
                return jointData;
            });
        }        

        /// <summary>
        /// Alpha computation for the low pass filter.
        /// </summary>
        /// <param name="rate">Data update rate in Hz.</param>
        /// <param name="cutoff">Cutoff frequency in Hz</param>
        /// <returns>Alpha value for low-pass filter.</returns>
        private Dictionary<JointType, Dictionary<String, Double>> Alpha(double rate,
            Dictionary<JointType, Dictionary<String, Double>> cutoff)
        {
            Dictionary<JointType, Dictionary<String, Double>> ret =
                new Dictionary<JointType, Dictionary<string, double>>();
            foreach (KeyValuePair<JointType, Dictionary<String, Double>> joint in cutoff) {
                Dictionary<String, Double> values = new Dictionary<string, double>();
                foreach (KeyValuePair<String, Double> entry in joint.Value) {
                    double tau = 1.0 / (2 * Math.PI * entry.Value);
                    double te = 1.0 / rate;
                    values.Add(entry.Key, 1.0 / (1.0 + tau / te));
                }
                ret.Add(joint.Key, values);
            }
            return ret;
        }

        /// <summary>
        /// Initliazies a dictionary of all joint types with a given value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Dictionary<JointType, Dictionary<String, Double>> InitCutoffDictionary(Double value)
        {
            Dictionary<JointType, Dictionary<String, Double>> ret =
                new Dictionary<JointType, Dictionary<string, double>>();
            foreach (JointType type in Enum.GetValues(typeof(JointType))) {
                Dictionary<String, Double> values = new Dictionary<string, double>();
                values.Add("X", value);
                values.Add("Y", value);
                values.Add("Z", value);
                ret.Add(type, values);
            }
            return ret;
        }

        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="e"></param>
        protected override void OnSmoothedFrameFinished(SmoothedFrameArrivedEventArgs e)
        {
            base.OnSmoothedFrameFinished(e);
        }

        /// <summary>
        /// Smoothing of noisy JointData with the 1€ Filter.
        /// </summary>
        /// <param name="jointData">Noisy JointData.</param>
        /// <returns>Smoothed JointData.</returns>
        public override JointData Smooth(JointData jointData)
        {
            Dictionary<JointType, Joint> dx = new Dictionary<JointType, Joint>();
            // if firstTime then
            if (firstTime) {
                // firstTime false
                firstTime = false;
                // dx = 0
                foreach (KeyValuePair<JointType, Joint> joint in jointData.Joints) {
                    Joint newJoint = joint.Value;
                    newJoint.Position.X = 0;
                    newJoint.Position.Y = 0;
                    newJoint.Position.Z = 0;
                    dx.Add(joint.Key, newJoint);
                }
            }
            // else
            else {
                // dx = (x - xfilt.hatxprev()) * rate
                rate = 1.0 / ((jointData.RelTimestamp - lastJointData.RelTimestamp) / 1000);
                // can be infinity after the first value
                rate = Double.IsInfinity(rate) ? 120 : rate;

                foreach (KeyValuePair<JointType, Joint> joint in jointData.Joints) {
                    Joint newJoint = joint.Value;
                    newJoint.Position.X = Convert.ToSingle((joint.Value.Position.X -
                        xfilt.Hatxprev[joint.Key].Position.X) * rate);
                    newJoint.Position.Y = Convert.ToSingle((joint.Value.Position.Y -
                        xfilt.Hatxprev[joint.Key].Position.Y) * rate);
                    newJoint.Position.Z = Convert.ToSingle((joint.Value.Position.Z -
                        xfilt.Hatxprev[joint.Key].Position.Z) * rate);
                    dx.Add(joint.Key, newJoint);
                }
            }
            // edx = dxfilt.filter(dx, alpha(rate, dcutoff))
            Dictionary<JointType, Joint> edx = dxfilt.Filter(dx, Alpha(rate, dcutoff));
            // cutoff = mincutoff + beta * |edx|
            Dictionary<JointType, Dictionary<String, Double>> cutoff =
                new Dictionary<JointType, Dictionary<string, double>>();
            foreach (KeyValuePair<JointType, Joint> joint in jointData.Joints) {
                Dictionary<string, double> values = new Dictionary<string, double>();
                values.Add("X", Mincutoff[joint.Key]["X"] + Beta * Math.Abs(edx[joint.Key].Position.X));
                values.Add("Y", Mincutoff[joint.Key]["Y"] + Beta * Math.Abs(edx[joint.Key].Position.Y));
                values.Add("Z", Mincutoff[joint.Key]["Z"] + Beta * Math.Abs(edx[joint.Key].Position.Z));
                cutoff.Add(joint.Key, values);
            }
            // return xfilt.filter(x, alpha(rate, cutoff ))
            Dictionary<JointType, Joint> x = new Dictionary<JointType, Joint>();
            foreach (KeyValuePair<JointType, Joint> joint in jointData.Joints) {
                Joint newJoint = joint.Value;
                x.Add(joint.Key, newJoint);
            }
            Dictionary<JointType, Joint> result = xfilt.Filter(x, Alpha(rate, cutoff));

            JointData newJointData = JointDataHandler.ReplaceJointsInJointData(
                jointData,
                DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond,
                result, OutputDataStreamType);

            lastJointData = jointData;
            return newJointData;
        }

        /// <summary>
        /// DEPRECATED
        /// </summary>
        /// <param name="jointData"></param>
        public override void Update(JointData jointData)
        {
            JointData smoothedJointData = Smooth(jointData);

            OnSmoothedFrameFinished(new SmoothedFrameArrivedEventArgs(jointData,
                smoothedJointData));
        }

        /// <summary>
        /// Slope of the function to determine the dynamic cutoff frequency.
        /// </summary>
        public Double Beta { get => beta; set => beta = value; }
        /// <summary>
        /// Minimum cutoff frequency of the the dynamic cutoff frequency.
        /// </summary>
        public double Fcmin
        {
            get => fcmin;
            set {
                fcmin = value;
                Mincutoff = InitCutoffDictionary(fcmin);
            }
        }
        /// <summary>
        /// Minimum cutoff frequency of the the dynamic cutoff frequency.
        /// </summary>
        public Dictionary<JointType, Dictionary<string, double>> Mincutoff
        {
            get => mincutoff;
            set => mincutoff = value;
        }
    }
}
