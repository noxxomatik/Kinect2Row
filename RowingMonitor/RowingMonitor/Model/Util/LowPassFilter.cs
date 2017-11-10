using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model.Util
{
    /// <summary>
    /// Implementation of a low pass filter.
    /// </summary>
    class LowPassFilter
    {
        Dictionary<JointType, Joint> hatxprev = new Dictionary<JointType, Joint>();
        bool firstTime = true;

        public LowPassFilter() { }        

        /// <summary>
        /// Apply the low pass filter.
        /// </summary>
        /// <param name="joints">The noisy joints to be filtered.</param>
        /// <param name="alpha">The amount of the preious values which influences the current filtered value.</param>
        /// <returns>The smoothed joints.</returns>
        public Dictionary<JointType, Joint> Filter(Dictionary<JointType, Joint> joints,
            Dictionary<JointType, Dictionary<String, Double>> alpha)
        {
            if (firstTime) {
                foreach (KeyValuePair<JointType, Joint> joint in joints) {
                    Hatxprev.Add(joint.Key, joint.Value);
                }
                firstTime = false;
            }
            Dictionary<JointType, Joint> hatx = new Dictionary<JointType, Joint>();
            foreach (KeyValuePair<JointType, Joint> joint in joints) {
                Joint filteredJoint = joint.Value;
                filteredJoint.Position.X = Convert.ToSingle(alpha[joint.Key]["X"] * joint.Value.Position.X +
                    (1 - alpha[joint.Key]["X"]) * Hatxprev[joint.Key].Position.X);

                filteredJoint.Position.Y = Convert.ToSingle(alpha[joint.Key]["Y"] * joint.Value.Position.Y +
                    (1 - alpha[joint.Key]["Y"]) * Hatxprev[joint.Key].Position.Y);

                filteredJoint.Position.Z = Convert.ToSingle(alpha[joint.Key]["Z"] * joint.Value.Position.Z +
                    (1 - alpha[joint.Key]["Z"]) * Hatxprev[joint.Key].Position.Z);
                hatx.Add(joint.Key, filteredJoint);
            }

            Hatxprev = hatx;

            return hatx;
        }

        /// <summary>
        /// Previous filtered values of all joints.
        /// </summary>
        public Dictionary<JointType, Joint> Hatxprev { get => hatxprev; private set => hatxprev = value; }
    }
}
