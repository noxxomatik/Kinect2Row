using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace RowingMonitor.Model
{
    /// <summary>
    /// Represents the arguments for a KinectReader's ColorFrameArrived event.
    /// </summary>
    public class ColorFrameArrivedEventArgs : EventArgs
    {
        private WriteableBitmap colorBitmap;

        public ColorFrameArrivedEventArgs(WriteableBitmap colorBitmap)
        {
            ColorBitmap = colorBitmap;
        }

        public WriteableBitmap ColorBitmap
        {
            get => colorBitmap;
            private set => colorBitmap = value;
        }
    }
}
