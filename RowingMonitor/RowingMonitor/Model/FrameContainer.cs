using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Diagnostics;

namespace RowingMonitor
{
    class FrameContainer
    {
        public struct Frame
        {
            public int index;
            public IReadOnlyDictionary<JointType, Joint> rawJoints;
            public Dictionary<JointType, Joint> smoothedJoints;
        }

        List<Frame> frames = new List<Frame>();

        public void AddFrame(IReadOnlyDictionary<JointType, Joint> joints)
        {
            Frame f = new Frame();
            f.rawJoints = joints;
            frames.Add(f);
        }

        public IReadOnlyDictionary<JointType, Joint> GetFrameRaw(int index)
        {
            return frames[index].rawJoints;
        }

        public List<Frame> GetAllFramesRaw()
        {
            return frames;
        }
    }
}
