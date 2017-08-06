using RowingMonitor.Model.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Controls;

namespace RowingMonitor.ViewModel
{
    public class HomeViewModel
    {
        private KinectReader kinectReader;
        private SkeletonFrontalDisplay skeletonFrontalDisplay;
        // remeber link to dispose it when not needed anymore
        private IDisposable displayLink;
        private IDisposable skeletonLink;
        private Timer timer;

        private Grid gUIGrid;        

        public HomeViewModel()
        {
            kinectReader = KinectReader.Instance;

            skeletonFrontalDisplay = new SkeletonFrontalDisplay(
                kinectReader.CoordinateMapper, kinectReader.DepthFrameDescription,
                kinectReader.ColorFrameDescription);

            displayLink = kinectReader.ColorFrameBlock.LinkTo(
                skeletonFrontalDisplay.ColorImageBlock);
            skeletonLink = kinectReader.JointDataBlock.LinkTo(
                skeletonFrontalDisplay.SkeletonBlock);            
        }

        private void Render(object state)
        {
            skeletonFrontalDisplay.Render();
        }

        public void ViewLoaded()
        {
            Frame frontalDisplayFrame = new Frame();
            frontalDisplayFrame.Content = skeletonFrontalDisplay.View;
            frontalDisplayFrame.SetValue(Grid.RowProperty, 0);
            GUIGrid.Children.Add(frontalDisplayFrame);

            kinectReader.StartReader();

            timer = new Timer(Render, null, 0, 40);
        }     
        
        public void ViewUnloaded()
        {
            timer.Dispose();
            skeletonLink.Dispose();
            displayLink.Dispose();
        }

        public Grid GUIGrid { get => gUIGrid; set => gUIGrid = value; }
    }
}
