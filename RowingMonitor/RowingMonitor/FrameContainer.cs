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
            public IReadOnlyDictionary<JointType, Joint> smoothedJoints;
        }

        List<Frame> frames = new List<Frame>();

        public void AddFrame(IReadOnlyDictionary<JointType, Joint> joints)
        {
            Frame f = new Frame();
            f.rawJoints = joints;
            frames.Add(f);

            Debug.WriteLine("chip: " + joints[JointType.SpineBase].Position.X + ", "
                                + joints[JointType.SpineBase].Position.Y + ", "
                                + joints[JointType.SpineBase].Position.Z);
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
