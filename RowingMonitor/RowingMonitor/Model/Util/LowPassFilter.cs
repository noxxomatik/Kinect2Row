using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RowingMonitor.Model
{
    class LowPassFilter
    {
        Dictionary<JointType, Joint> hatxprev = new Dictionary<JointType, Joint>();
        bool firstTime = true;

        public Dictionary<JointType, Joint> Hatxprev { get => hatxprev; }

        public LowPassFilter() { }

        public Dictionary<JointType, Joint> Filter(Dictionary<JointType, Joint> joints,
            Dictionary<JointType, Dictionary<String, Double>> alpha)
        {
            if (firstTime) {
                foreach (KeyValuePair<JointType, Joint> joint in joints) {
                    hatxprev.Add(joint.Key, joint.Value);
                }
                firstTime = false;
            }
            Dictionary<JointType, Joint> hatx = new Dictionary<JointType, Joint>();
            foreach (KeyValuePair<JointType, Joint> joint in joints) {
                Joint filteredJoint = joint.Value;
                filteredJoint.Position.X = Convert.ToSingle(alpha[joint.Key]["X"] * joint.Value.Position.X +
                    (1 - alpha[joint.Key]["X"]) * hatxprev[joint.Key].Position.X);

                filteredJoint.Position.Y = Convert.ToSingle(alpha[joint.Key]["Y"] * joint.Value.Position.Y +
                    (1 - alpha[joint.Key]["Y"]) * hatxprev[joint.Key].Position.Y);

                filteredJoint.Position.Z = Convert.ToSingle(alpha[joint.Key]["Z"] * joint.Value.Position.Z +
                    (1 - alpha[joint.Key]["Z"]) * hatxprev[joint.Key].Position.Z);
                hatx.Add(joint.Key, filteredJoint);
            }
            return hatx;
        }
    }
}
